using System.Text.RegularExpressions;

namespace ThirteenIsh.Game;

/// <summary>
/// Helper class for parsing dice roll working strings to extract natural die roll values.
/// </summary>
internal static partial class DiceRollHelper
{
    [GeneratedRegex(@"(\d+)d(\d+) 🎲 (\d+)")]
    private static partial Regex D20Regex();

    /// <summary>
    /// Extracts the natural d20 roll value from a working string.
    /// Handles basic rolls, rolls with bonuses, and reroll scenarios.
    /// </summary>
    /// <param name="working">The working string from a dice roll evaluation</param>
    /// <returns>The natural d20 roll value (1-20), or null if parsing fails</returns>
    public static int ExtractNaturalD20Roll(string working)
    {
        ArgumentNullException.ThrowIfNull(working);

        // Look for the pattern "1d20 🎲 {result}" or "1d20 🎲 {result} [...]"
        // The result after 🎲 is what we want for single die rolls
        var match = D20Regex().Match(working);
        if (!match.Success ||
            !int.TryParse(match.Groups[1].Value, out var dieCount) || dieCount != 1 ||
            !int.TryParse(match.Groups[2].Value, out var dieSides) || dieSides != 20 ||
            !int.TryParse(match.Groups[3].Value, out var dieRoll))
        {
            throw new ArgumentException($"Working string '{working}' does not begin with a roll of 1d20.");
        }

        return dieRoll;
    }
}