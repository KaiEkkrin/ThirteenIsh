using Discord;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ThirteenIsh.Game;

public static partial class AttributeName
{
    [GeneratedRegex(@"(\p{L})(\p{L}*)", RegexOptions.Compiled)]
    private static partial Regex NamePartRegex();

    [GeneratedRegex(@"[^\p{L}\s]", RegexOptions.Compiled)]
    private static partial Regex NotNameRegex();

    // A tag begins with a letter and contains any combination of letters, digits and white space,
    // without any white space at the end.
    [GeneratedRegex(@"^\s*(\p{L}(?:[\p{L}\p{N}\s]*[\p{L}\p{N}])?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhiteSpaceRegex();

    /// <summary>
    /// Adds a character selection option to a command.
    /// </summary>
    public static SlashCommandOptionBuilder AddCharacterOption(
        this SlashCommandOptionBuilder builder, string name = "character")
    {
        return builder.AddOption(new SlashCommandOptionBuilder()
            .WithName(name)
            .WithDescription("The character name.")
            .WithRequired(true) // TODO in future, have none use the user's current active character in the guild (?)
            .WithType(ApplicationCommandOptionType.String));
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

    /// <summary>
    /// Canonicalizes a tag, if possible. (Not directly related to the other methods here)
    /// </summary>
    public static bool TryCanonicalizeTag(string value, [MaybeNullWhen(false)] out string tagValue)
    {
        var match = TagRegex().Match(value);
        if (match.Success)
        {
            tagValue = WhiteSpaceRegex().Replace(match.Groups[1].Value, " ");
            return true;
        }

        tagValue = null;
        return false;
    }

    private static bool TryCanonicalizeInternal(string name, MatchEvaluator whiteSpaceEvaluator,
        [MaybeNullWhen(false)] out string canonicalizedName)
    {
        if (NotNameRegex().IsMatch(name))
        {
            canonicalizedName = null;
            return false;
        }

        name = WhiteSpaceRegex().Replace(name, whiteSpaceEvaluator);
        canonicalizedName = NamePartRegex().Replace(name, match =>
            match.Groups[1].Value.ToUpper(CultureInfo.CurrentCulture) +
            match.Groups[2].Value.ToLower(CultureInfo.CurrentCulture));

        return true;
    }
}
