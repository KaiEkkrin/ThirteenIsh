using Discord;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ThirteenIsh.Game;

internal static partial class AttributeName
{
    [GeneratedRegex(@"(\p{L})(\p{L}*)", RegexOptions.Compiled)]
    private static partial Regex NamePartRegex();

    [GeneratedRegex(@"[^\p{L}\s]", RegexOptions.Compiled)]
    private static partial Regex NotNameRegex();

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
            match.Groups[1].Value.ToUpper(CultureInfo.CurrentCulture) +
            match.Groups[2].Value.ToLower(CultureInfo.CurrentCulture));

        return true;
    }
}
