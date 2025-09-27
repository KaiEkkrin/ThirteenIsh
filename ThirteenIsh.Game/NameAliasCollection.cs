
using System.Text.RegularExpressions;

namespace ThirteenIsh.Game;

/// <summary>
/// Enables us to generate uniquely identifying, short aliases for character and
/// monster names.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not end in Collection", Justification = "This class manages a collection of name aliases and the Collection suffix is appropriate")]
public sealed partial class NameAliasCollection
{
    private const int DefaultPrefixTryCount = 16;

    [GeneratedRegex(@"^(\p{L}+)([0-9]*)$", RegexOptions.Compiled)]
    public static partial Regex AliasRegex();

    [GeneratedRegex(@"\p{Lu}\p{Ll}*", RegexOptions.Compiled)]
    private static partial Regex AliasPartRegex();

    // We split the existing aliases into a mapping of prefix => numbers in use.
    // I'll always prefer a lower number (if there needs to be one at all.)
    private readonly Dictionary<string, NumbersInUse> _numbersInUseDictionary = [];

    // We also map each original name to the prefix we're using for it
    private readonly Dictionary<string, string> _prefixesByNameDictionary = [];

    // Try not to repeatedly re-compute the split prefixes
    // (remember to keep this in sync with `_prefixesByNameDictionary`)
    private List<string[]>? _splitPrefixes;

    private readonly int _prefixTryCount;

    public NameAliasCollection(IEnumerable<(string Alias, string Name)> aliases, int prefixTryCount = DefaultPrefixTryCount)
    {
        foreach (var (alias, name) in aliases)
        {
            if (!AttributeName.TryCanonicalizeMultiPart(name, out var canonicalizedName))
                throw new ArgumentException($"Name '{name}' cannot be canonicalized", nameof(aliases));

            var match = AliasRegex().Match(alias);
            if (!match.Success) throw new ArgumentException($"Invalid alias: '{alias}'", nameof(aliases));

            var number = match.Groups[2].Value is not { Length: > 0 } numberString
                ? 0
                : int.TryParse(numberString, out var numberValue)
                    ? numberValue
                    : throw new ArgumentException($"Invalid alias number: '{alias}'", nameof(aliases));

            var prefix = match.Groups[1].Value;
            AddToDictionaries(prefix, canonicalizedName, number);
        }

        _prefixTryCount = prefixTryCount;
    }

    public NameAliasCollection(Encounter encounter) : this(encounter.Combatants.Select(c => (c.Alias, c.Name)))
    {
    }

    /// <summary>
    /// The aliases in order.
    /// </summary>
    public IEnumerable<string> Aliases => _numbersInUseDictionary
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

        var prefix = GeneratePrefixAndNumber(name, prefixLength, alwaysAddNumber, out var number);
        var alias = number == 0 ? prefix : $"{prefix}{number}";
        AddToDictionaries(prefix, name, number);
        return alias;
    }

    /// <summary>
    /// Checks how ambiguous this alias is, in terms of the current name alias collection.
    /// </summary>
    /// <param name="alias">The alias.</param>
    /// <returns>The number of different tracked names it could map to, or 0 if this alias is
    /// not tracked.</returns>
    public int CheckAmbiguity(string alias)
    {
        var match = AliasRegex().Match(alias);
        if (!match.Success) throw new ArgumentException("Not a valid alias", nameof(alias));
        return GetAmbiguity(match.Groups[1].Value);
    }

    /// <summary>
    /// For unit tests only.
    /// </summary>
    public static bool CouldBeAliasFor(string alias, string name)
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

    private void AddPossiblePrefixes(string name, int prefixLength, bool alwaysAddNumber, List<PossibleAlias> list)
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
        var totalPartsLength = nameParts[0].Length + cumulativeLengths[0];
        prefixLength = Math.Min(prefixLength, totalPartsLength);

        // Start the recursive stage
        StringBuilder aliasBuilder = new(prefixLength);
        AddPossiblePrefixParts(0);
        return;

        bool AddPossiblePrefixParts(int index)
        {
            if (index == nameParts.Length || prefixLength == 0)
            {
                // This is a possible prefix; generate the alias candidate
                var aliasString = aliasBuilder.ToString();
                PossibleAlias possibleAlias = new(GetAmbiguity(aliasString),
                    FindSmallestNumberForPrefix(aliasString, alwaysAddNumber), list.Count, aliasString);
                list.Add(possibleAlias);
                return list.Count < _prefixTryCount;
            }

            // Always leave room for the remaining name parts to contribute at least one character each
            // to the alias:
            var remainingPartsCount = nameParts.Length - (index + 1);
            var remainingPrefixLength = prefixLength - aliasBuilder.Length;
            if (remainingPartsCount > remainingPrefixLength) throw new InvalidOperationException(
                $"At {index}: found {remainingPartsCount} remaining parts but only {remainingPrefixLength} prefix length remaining");

            var lengthMaxBound = Math.Min(remainingPrefixLength - remainingPartsCount, nameParts[index].Length);

            // The length of this part's alias must always be long enough that the remaining name parts
            // could be aliased to a string that fills the rest of the alias up to the prefix length
            var lengthMinBound = Math.Max(1, remainingPrefixLength - cumulativeLengths[index]);
            if (lengthMinBound > lengthMaxBound)
                throw new InvalidOperationException($"At {index}: got bad length bounds {lengthMinBound}, {lengthMaxBound}");

            // Rather than starting at one of the bounds, try the midpoint between them first and work
            // outwards in both directions
            var lengthBoundsMidpoint = (lengthMaxBound + lengthMinBound) / 2;
            for (int i = lengthBoundsMidpoint, j = lengthBoundsMidpoint + 1;
                 i >= lengthMinBound || j <= lengthMaxBound;
                 --i, ++j)
            {
                if (i >= lengthMinBound)
                {
                    var prefix = nameParts[index][..i];
                    aliasBuilder.Append(prefix);
                    var keepGoing = AddPossiblePrefixParts(index + 1);
                    aliasBuilder.Remove(aliasBuilder.Length - prefix.Length, prefix.Length);
                    if (!keepGoing) return false;
                }

                if (j <= lengthMaxBound)
                {
                    var prefix = nameParts[index][..j];
                    aliasBuilder.Append(prefix);
                    var keepGoing = AddPossiblePrefixParts(index + 1);
                    aliasBuilder.Remove(aliasBuilder.Length - prefix.Length, prefix.Length);
                    if (!keepGoing) return false;
                }
            }

            return true;
        }
    }

    private void AddToDictionaries(string prefix, string name, int number)
    {
        if (!_prefixesByNameDictionary.TryGetValue(name, out var currentPrefix))
        {
            _prefixesByNameDictionary.Add(name, prefix);
            _splitPrefixes?.Add(name.Split(' '));
        }
        else if (currentPrefix != prefix)
        {
            throw new InvalidOperationException(
                $"Saw '{name}' with an alias starting '{prefix}' but have already tracked it using the prefix '{currentPrefix}'");
        }

        if (_numbersInUseDictionary.TryGetValue(prefix, out var numbersInUse))
        {
            numbersInUse.AddNumber(number);
        }
        else
        {
            numbersInUse = new NumbersInUse(number);
            _numbersInUseDictionary.Add(prefix, numbersInUse);
        }
    }

    private int FindSmallestNumberForPrefix(string prefix, bool alwaysAddNumber)
    {
        var lowestNumber = alwaysAddNumber ? 1 : 0;
        if (!_numbersInUseDictionary.TryGetValue(prefix, out var existingNumbers))
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

    private string GeneratePrefixAndNumber(string name, int prefixLength, bool alwaysAddNumber, out int number)
    {
        // If we're already tracking a prefix in association with this name, re-use it
        if (_prefixesByNameDictionary.TryGetValue(name, out var currentPrefix))
        {
            number = FindSmallestNumberForPrefix(currentPrefix, alwaysAddNumber);
            return currentPrefix;
        }

        // There could be a very large number of possible prefixes. We'll only
        // look at the top few. For each one, find the smallest number for this prefix.
        // Use the least ambiguous alias, with the smallest number, in a deterministic order.
        List<PossibleAlias> list = [];
        AddPossiblePrefixes(name, prefixLength, alwaysAddNumber, list);

        // TODO reproduce in the test suite the case of no possible alias being available,
        // and handle it. (I'm not really sure how to get to this.)
        var topAlias = list.MinBy(x => x, PossibleAliasComparer.Instance);
        if (topAlias is null) throw new InvalidOperationException(
            $"No possible alias for '{name}' with prefix length {prefixLength}");

        number = topAlias.Number;
        return topAlias.Prefix;
    }

    private int GetAmbiguity(string prefix)
    {
        var ambiguity = 0;
        _splitPrefixes ??= _prefixesByNameDictionary.Keys.Select(name => name.Split(' ')).ToList();
        foreach (var nameParts in _splitPrefixes)
        {
            if (CouldBeAliasFor(prefix, nameParts)) ++ambiguity;
        }

        return ambiguity;
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

    private record PossibleAlias(int Ambiguity, int Number, int EnumerationOrder, string Prefix);

    private sealed class PossibleAliasComparer : Comparer<PossibleAlias>
    {
        public static readonly PossibleAliasComparer Instance = new();

        private PossibleAliasComparer()
        {
        }

        public override int Compare(PossibleAlias? x, PossibleAlias? y) => (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => x.Ambiguity.CompareTo(y.Ambiguity) is not 0 and var ambiguityCmp
                ? ambiguityCmp
                : x.Number.CompareTo(y.Number) is not 0 and var numberCmp
                    ? numberCmp
                    : x.EnumerationOrder.CompareTo(y.EnumerationOrder) is not 0 and var enumerationOrderCmp
                        ? enumerationOrderCmp
                        : string.CompareOrdinal(x.Prefix, y.Prefix)
        };
    }
}
