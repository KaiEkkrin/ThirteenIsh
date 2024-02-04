namespace ThirteenIsh.Parsing;

/// <summary>
/// Parses a dice roll sub-expression.
/// </summary>
internal sealed class DiceRollParser : ParserBase
{
    private const int MaxDiceCount = 100;
    private const int MaxDiceSize = 10_000;

    public static readonly DiceRollParser Instance = new();

    private static readonly SingleCharacterParser DParser = new(nameof(DiceRollParser), 'd', 'D');
    private static readonly SingleCharacterParser KeepParser = new(nameof(DiceRollParser), 'k', 'K', 'l', 'L');

    public override ParseTreeBase Parse(string input, int offset, int depth)
    {
        CheckMaxDepth(offset, ref depth);

        // Parse the dice count
        var diceCount = IntegerParser.Instance.Parse(input, offset, depth);
        if (!string.IsNullOrEmpty(diceCount.Error) || diceCount.LiteralValue > MaxDiceCount ||
            diceCount.LiteralValue < 1)
            return diceCount;

        // Parse the "d"
        var d = DParser.Parse(input, diceCount.Offset, depth);
        if (!string.IsNullOrEmpty(d.Error)) return d;

        // Parse the dice size
        var diceSize = IntegerParser.Instance.Parse(input, d.Offset, depth);
        if (!string.IsNullOrEmpty(diceSize.Error) || diceSize.LiteralValue > MaxDiceSize ||
            diceSize.LiteralValue < 1)
            return diceSize;

        // Parse an optional `k<count>` or `l<count>`
        ParseTreeBase? keepValue = null;
        var keep = KeepParser.Parse(input, diceSize.Offset, depth);
        var lastOffset = diceSize.Offset;
        int? keepHighest = null, keepLowest = null;
        if (string.IsNullOrEmpty(keep.Error))
        {
            keepValue = IntegerParser.Instance.Parse(input, keep.Offset, depth);
            if (!string.IsNullOrEmpty(keepValue.Error) ||
                keepValue.LiteralValue > diceCount.LiteralValue ||
                keepValue.LiteralValue < 1)
                return keepValue;

            lastOffset = keepValue.Offset;
            switch (keep.Operator)
            {
                case 'k' or 'K':
                    keepHighest = keepValue.LiteralValue;
                    break;

                case 'l' or 'L':
                    keepLowest = keepValue.LiteralValue;
                    break;
            }
        }

        return new DiceRollParseTree(
            lastOffset, diceCount.LiteralValue, diceSize.LiteralValue, keepHighest, keepLowest);
    }
}
