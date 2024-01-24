using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ThirteenIsh.Parsing;

[DebuggerDisplay("{DiceCount}d{DiceSize}")]
internal sealed class DiceRollParseTree(int offset, int diceCount, int diceSize)
    : ParseTreeBase(offset)
{
    public int DiceCount => diceCount;
    public int DiceSize => diceSize;

    public override int Evaluate(out string working)
    {
        var value = 0;
        StringBuilder workingBuilder = new();
        for (var i = 0; i < diceCount; ++i)
        {
            // This RNG should be good enough, with the recent improvements
            var roll = Random.Shared.Next(1, diceSize + 1);
            value += roll;

            if (diceCount <= 1) continue;
            if (i > 0) workingBuilder.Append(" + ");
            workingBuilder.Append(CultureInfo.CurrentCulture, $"{roll}");
        }

        working = diceCount > 1
            ? $"{diceCount}d{diceSize} 🎲 {value} [{workingBuilder}]"
            : $"{diceCount}d{diceSize} 🎲 {value}";

        return value;
    }

    public override string ToString() => $"{diceCount}d{diceSize}";
}
