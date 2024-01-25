namespace ThirteenIsh.Parsing;

/// <summary>
/// Parses a dice roll sub-expression.
/// </summary>
internal sealed class DiceRollParser : ParserBase
{
    public static readonly DiceRollParser Instance = new();

    private static readonly SingleCharacterParser DParser = new(nameof(DiceRollParser), 'd', 'D');

    public override ParseTreeBase Parse(string input, int offset)
    {
        // Parse the dice count
        var diceCount = IntegerParser.Instance.Parse(input, offset);
        if (!string.IsNullOrEmpty(diceCount.Error)) return diceCount;

        // Parse the "d"
        var d = DParser.Parse(input, diceCount.Offset);
        if (!string.IsNullOrEmpty(d.Error)) return d;

        // Parse the dice size
        var diceSize = IntegerParser.Instance.Parse(input, d.Offset);
        if (!string.IsNullOrEmpty(diceSize.Error)) return diceSize;

        return new DiceRollParseTree(diceSize.Offset, diceCount.LiteralValue, diceSize.LiteralValue);
    }
}
