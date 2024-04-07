using System.Globalization;
using System.Text;
using CharacterType = ThirteenIsh.Database.Entities.CharacterType;

namespace ThirteenIsh;

public static class CharacterTypeExtensions
{
    public static string FriendlyName(this CharacterType characterType,
        FriendlyNameOptions options = FriendlyNameOptions.None)
    {
        // This is ridiculously overwrought :)
        var stringBuilder = characterType switch
        {
            CharacterType.PlayerCharacter => new StringBuilder("character"),
            CharacterType.Monster => new StringBuilder("monster"),
            _ => throw new ArgumentException("Unrecognised character type", nameof(characterType))
        };

        if (options.HasFlag(FriendlyNameOptions.CapitalizeFirstCharacter))
        {
            stringBuilder[0] = char.ToUpper(stringBuilder[0], CultureInfo.CurrentCulture);
        }

        if (options.HasFlag(FriendlyNameOptions.Plural))
        {
            stringBuilder.Append('s');
        }

        return stringBuilder.ToString();
    }
}

[Flags]
public enum FriendlyNameOptions
{
    None = 0,
    CapitalizeFirstCharacter = 1,
    Plural = 2
}
