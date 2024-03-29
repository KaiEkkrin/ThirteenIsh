using System.Text.RegularExpressions;

namespace ThirteenIsh.Game;

/// <summary>
/// Enables us to generate uniquely identifying, short aliases for character and
/// monster names.
/// </summary>
internal sealed partial class NameAliasCollection
{
    [GeneratedRegex(@"^(\p{L}+)([0-9]*)$", RegexOptions.Compiled)]
    internal static partial Regex AliasRegex();

    [GeneratedRegex(@"\p{Lu}\p{Ll}*", RegexOptions.Compiled)]
    private static partial Regex AliasPartRegex();

    // We split the existing aliases into a mapping of prefix => numbers in use.
    // I'll always prefer a lower number (if there needs to be one at all.)
    private readonly Dictionary<string, NumbersInUse> _aliasDictionary = [];

    public NameAliasCollection(IEnumerable<string> aliases)
    {
        foreach (var alias in aliases)
        {
            var match = AliasRegex().Match(alias);
            if (!match.Success) throw new ArgumentException($"Invalid alias: {alias}", nameof(aliases));

            var number = match.Groups[2].Value is not { Length: > 0 } numberString
                ? 0
                : int.TryParse(numberString, out var numberValue)
                    ? numberValue
                    : throw new ArgumentException($"Invalid alias number: {alias}", nameof(aliases));

            var prefix = match.Groups[1].Value;
            AddToDictionary(prefix, number);
        }
    }

    /// <summary>
    /// The aliases in order.
    /// </summary>
    public IEnumerable<string> Aliases => _aliasDictionary
        .SelectMany(pair => pair.Value.Numbers.Select(number => (Prefix: pair.Key, Number: number)))
        .OrderBy(x => x.Prefix)
        .ThenBy(x => x.Number)
        .Select(x => x.Number == 0 ? x.Prefix : $"{x.Prefix}{x.Number}");

    /// <summary>
    /// Adds a new alias for this name to the collection, and returns it.
    /// </summary>
    public string Add(string name, int prefixLength, bool alwaysAddNumber)
    {
        if (prefixLength > 10) throw new ArgumentException("Prefix length too long", nameof(prefixLength));

        name = AttributeName.TryCanonicalizeMultiPart(name, out var canonicalizedName)
            ? canonicalizedName
            : throw new ArgumentException("Name cannot be canonicalized", nameof(name));

        // There could be a very large number of possible prefixes. We'll only
        // look at the top few. For each one, find the smallest number for this prefix.
        // Use the smallest number, using the earliest in the enumeration order if there was a tie.
        List<string> prefixes = [];
        AddPossiblePrefixes(name, prefixLength, prefixes, 12);

        // TODO avoid the Linq here for better performance?
        var possibleResults = prefixes
            .Select((prefix, index) =>
                (Prefix: prefix, Number: FindSmallestNumberForPrefix(prefix, alwaysAddNumber), EnumerationOrder: index))
            .OrderBy(x => x.Number)
            .ThenBy(x => x.EnumerationOrder);

        var (topPrefix, topNumber, _) = possibleResults.First();
        var alias = topNumber == 0 ? topPrefix : $"{topPrefix}{topNumber}";
        AddToDictionary(topPrefix, topNumber);
        return alias;
    }

    internal static bool CouldBeAliasFor(string alias, string name)
    {
        name = AttributeName.TryCanonicalizeMultiPart(name, out var canonicalizedName)
            ? canonicalizedName
            : throw new ArgumentException("Name cannot be canonicalized", nameof(name));

        return CouldBeAliasFor(alias, name.Split(' '));
    }

    /// <summary>
    /// Checks whether `alias` could be an alias for `nameParts`. Assumes that `nameParts` is a valid
    /// canonicalized multi-part name that has been split into parts and that `alias` is a valid alias
    /// (because we're likely to be calling this a lot.)
    /// </summary>
    private static bool CouldBeAliasFor(string alias, string[] nameParts)
    {
        if (alias.Length == 0 || nameParts.Length == 0) return false;

        var i = 0;
        foreach (var match in AliasPartRegex().EnumerateMatches(alias))
        {
            if (i >= nameParts.Length) return false; // too many alias parts
            if (nameParts[i].Length < match.Length) return false; // this name part is too long so can't match
            if (nameParts[i][..match.Length] != alias[match.Index..(match.Index + match.Length)]) return false;
            ++i;
        }

        // Not enough alias parts is OK, some names are too long to fit
        return true;
    }

    private static void AddPossiblePrefixes(string name, int prefixLength, List<string> list, int maxCount)
    {
        // Only take into account as many name parts as there is a prefix length (the minimum
        // contribution from each is one character.) So long as `prefixLength` is reasonably
        // short this serves as a cap on recursion
        var nameParts = name.Split(' ');
        if (nameParts.Length == 0) return;
        if (nameParts.Length > prefixLength) nameParts = nameParts[..prefixLength];

        // Work out the cumulative lengths of the following name parts, working backwards from the end.
        var cumulativeLengths = new int[nameParts.Length];
        cumulativeLengths[^1] = 0;
        for (var i = nameParts.Length - 1; i >= 1; --i)
        {
            cumulativeLengths[i - 1] = cumulativeLengths[i] + nameParts[i].Length;
        }

        // Bound the prefix length to the total length of the name parts (otherwise we'll never
        // find a solution for too-short names)
        var totalPartsLength = nameParts.Length == 1 ? nameParts[0].Length : nameParts[0].Length + cumulativeLengths[0];
        prefixLength = Math.Min(prefixLength, totalPartsLength);

        // Start the recursive stage
        AddPossiblePrefixParts(0, string.Empty);
        return;

        bool AddPossiblePrefixParts(int index, string acc)
        {
            if (index == nameParts.Length || prefixLength == 0)
            {
                // This is a possible prefix
                list.Add(acc);
                return list.Count < maxCount;
            }

            // Always leave room for the remaining name parts to contribute at least one character each
            // to the alias:
            var remainingPartsCount = nameParts.Length - (index + 1);
            var remainingPrefixLength = prefixLength - acc.Length;
            if (remainingPartsCount > remainingPrefixLength) throw new InvalidOperationException(
                $"At {index}: found {remainingPartsCount} remaining parts but only {remainingPrefixLength} prefix length remaining");

            var startingLength = Math.Min(remainingPrefixLength - remainingPartsCount, nameParts[index].Length);
            for (var length = startingLength; length >= 1; --length)
            {
                // If this length is too short to be able to fill the rest of the prefix to the required
                // length from the remaining name parts, stop
                if (length + cumulativeLengths[index] < remainingPrefixLength) break;

                var prefix = nameParts[index][..length];
                if (!AddPossiblePrefixParts(index + 1, acc + prefix)) return false;
            }

            return true;
        }
    }

    private void AddToDictionary(string prefix, int number)
    {
        if (_aliasDictionary.TryGetValue(prefix, out var numbersInUse))
        {
            numbersInUse.AddNumber(number);
        }
        else
        {
            numbersInUse = new NumbersInUse(number);
            _aliasDictionary.Add(prefix, numbersInUse);
        }
    }

    private int FindSmallestNumberForPrefix(string prefix, bool alwaysAddNumber)
    {
        var lowestNumber = alwaysAddNumber ? 1 : 0;
        if (!_aliasDictionary.TryGetValue(prefix, out var existingNumbers))
        {
            return lowestNumber;
        }

        var maxExistingNumber = existingNumbers.MaxValue;
        for (var i = lowestNumber; i < maxExistingNumber; ++i)
        {
            if (existingNumbers.Contains(i)) continue;
            return i;
        }

        return maxExistingNumber + 1;
    }

    private sealed class NumbersInUse(int number)
    {
        private readonly HashSet<int> _numbers = [number];

        public bool Contains(int number) => _numbers.Contains(number);

        public int MaxValue { get; private set; } = number;

        public IEnumerable<int> Numbers => _numbers;

        public void AddNumber(int number)
        {
            _numbers.Add(number);
            MaxValue = Math.Max(number, MaxValue);
        }
    }
}
