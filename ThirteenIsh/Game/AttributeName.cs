using System.Diagnostics.CodeAnalysis;
using System.Text;
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

    [GeneratedRegex(@"\s")]
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
        name = WhiteSpaceRegex().Replace(name, string.Empty);
        StringBuilder builder = new();
        foreach (var ch in name)
        {
            if (char.IsLetter(ch))
            {
                builder.Append(builder.Length == 0 ? char.ToUpperInvariant(ch) : char.ToLowerInvariant(ch));
            }
            else
            {
                canonicalizedName = null;
                return false;
            }
        }

        canonicalizedName = builder.ToString();
        return true;
    }


}
