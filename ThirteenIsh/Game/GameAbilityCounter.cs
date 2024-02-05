using System.Globalization;

namespace ThirteenIsh.Game;

/// <summary>
/// A convenient way of declaring counters with 3-letter capitalised aliases
/// commonly used for e.g. ability scores.
/// </summary>
internal class GameAbilityCounter(string name, int defaultValue = 10, int minValue = 1, int maxValue = 30)
    : GameCounter(name, GetAlias(name), defaultValue, minValue, maxValue)
{
    private static string GetAlias(string name) => name[..3].ToUpper(CultureInfo.CurrentCulture);
}
