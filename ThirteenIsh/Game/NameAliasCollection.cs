using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ThirteenIsh.Game;

/// <summary>
/// Enables us to generate uniquely identifying, short aliases for character and
/// monster names.
/// </summary>
internal sealed partial class NameAliasCollection
{
    [GeneratedRegex(@"^(\p{L}+)([0-9]*)$", RegexOptions.Compiled)]
    private static partial Regex AliasRegex();

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
        var possibleResults = EnumeratePossiblePrefixes(name, prefixLength)
            .Take(12)
            .Select((prefix, index) =>
                (Prefix: prefix, Number: FindSmallestNumberForPrefix(prefix, alwaysAddNumber), EnumerationOrder: index))
            .OrderBy(x => x.Number)
            .ThenBy(x => x.EnumerationOrder);

        var (topPrefix, topNumber, _) = possibleResults.First();
        var alias = topNumber == 0 ? topPrefix : $"{topPrefix}{topNumber}";
        AddToDictionary(topPrefix, topNumber);
        return alias;
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

    private static IEnumerable<string> EnumeratePossiblePrefixes(string name, int prefixLength)
    {
        // Only take into account as many name parts as there is a prefix length (the minimum
        // contribution from each is one character.) So long as `prefixLength` is reasonably
        // short this serves as a cap on recursion
        var nameParts = name.Split(' ');
        if (nameParts.Length > prefixLength) nameParts = nameParts[..prefixLength];

        // TODO try to make the following logic non-recursive?
        return EnumeratePossiblePrefixParts(nameParts, 0, prefixLength);
    }

    private static IEnumerable<string> EnumeratePossiblePrefixParts(string[] nameParts, int index, int prefixLength)
    {
        if (index == nameParts.Length || prefixLength == 0)
        {
            // I need to yield something in order for the higher recursions to do the same :)
            yield return string.Empty;
            yield break;
        }

        // Always leave room for the remaining name parts to contribute at least one character each
        // to the alias:
        var remainingPartsCount = nameParts.Length - (index + 1);
        if (remainingPartsCount > prefixLength) throw new InvalidOperationException(
            $"Found {remainingPartsCount} remaining parts but only {prefixLength} prefix length");

        var startingLength = Math.Min(prefixLength - remainingPartsCount, nameParts[index].Length);
        for (var length = startingLength; length >= 1; --length)
        {
            var prefix = nameParts[index][..length];
            foreach (var suffix in EnumeratePossiblePrefixParts(nameParts, index + 1, prefixLength - length))
            {
                yield return prefix + suffix;
            }
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
