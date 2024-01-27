using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ThirteenIsh.Game;

internal static partial class AttributeName
{
    public static readonly IReadOnlyList<string> AbilityScores = [
        "Strength",
        "Dexterity",
        "Constitution",
        "Intelligence",
        "Wisdom",
        "Charisma"
    ];

    public static readonly IReadOnlyList<string> Classes = [
        "Barbarian",
        "Bard",
        "Cleric",
        "Fighter",
        "Paladin",
        "Ranger",
        "Rogue",
        "Sorcerer",
        "Wizard"
    ];

    [GeneratedRegex(@"(\p{L})(\p{L}*)")]
    private static partial Regex NamePartRegex();

    [GeneratedRegex(@"[^\p{L}\s]")]
    private static partial Regex NotNameRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();

    /// <summary>
    /// Finds an attribute name that matches the input name including unambiguous string overlap.
    /// </summary>
    public static bool FindMatching(string name, IReadOnlyCollection<string> attributeNames,
        [MaybeNullWhen(false)] out string attributeName)
    {
        name = WhiteSpaceRegex().Replace(name, string.Empty);
        attributeName = null;
        foreach (var attribute in attributeNames)
        {
            if (attribute.StartsWith(name, StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith(attribute, StringComparison.OrdinalIgnoreCase))
            {
                if (attributeName != null)
                {
                    // This is ambiguous
                    return false;
                }

                attributeName = attribute;
            }
        }

        return attributeName != null;
    }

    /// <summary>
    /// Gets an input attribute name into canonical form.
    /// </summary>
    public static bool TryCanonicalize(string name, [MaybeNullWhen(false)] out string canonicalizedName)
    {
        return TryCanonicalizeInternal(name, _ => string.Empty, out canonicalizedName);
    }

    /// <summary>
    /// Gets an input multi-part name (e.g. a character name) into canonical form.
    /// </summary>
    public static bool TryCanonicalizeMultiPart(string name,
        [MaybeNullWhen(false)] out string canonicalizedName)
    {
        return TryCanonicalizeInternal(
            name, match =>
            {
                // Here I want to replace white space at the beginning or end of the name with the
                // empty string and white space in the middle with a single space character
                return match.Index == 0 || (match.Index + match.Length) == name.Length
                    ? string.Empty
                    : " ";
            },
            out canonicalizedName);
    }

    private static bool TryCanonicalizeInternal(string name, MatchEvaluator whiteSpaceEvaluator,
        [MaybeNullWhen(false)] out string canonicalizedName)
    {
        name = WhiteSpaceRegex().Replace(name, whiteSpaceEvaluator);
        if (NotNameRegex().IsMatch(name))
        {
            canonicalizedName = null;
            return false;
        }

        canonicalizedName = NamePartRegex().Replace(name, match =>
            match.Groups[1].Value.ToUpperInvariant() + match.Groups[2].Value.ToLowerInvariant());

        return true;
    }
}
