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
        var rawDiceCount = IntegerParser.Instance.Parse(input, offset, depth);
        if (!string.IsNullOrEmpty(rawDiceCount.Error))
            return rawDiceCount;

        var (diceCount, diceSign) = rawDiceCount.LiteralValue < 0
            ? (-rawDiceCount.LiteralValue, -1)
            : (rawDiceCount.LiteralValue, 1);

        if (diceCount == 0 || diceCount > MaxDiceCount)
            return new ErrorParseTree(offset, $"Invalid dice count: {diceCount}");

        // Parse the "d"
        var d = DParser.Parse(input, rawDiceCount.Offset, depth);
        if (!string.IsNullOrEmpty(d.Error)) return d;

        // Parse the dice size
        var diceSize = IntegerParser.Instance.Parse(input, d.Offset, depth);
        if (!string.IsNullOrEmpty(diceSize.Error))
            return diceSize;

        if (diceSize.LiteralValue < 1 || diceSize.LiteralValue > MaxDiceSize)
            return new ErrorParseTree(d.Offset, $"Invalid dice size: {diceSize.LiteralValue}");

        // Parse an optional `k<count>` or `l<count>`
        ParseTreeBase? keepValue;
        var keep = KeepParser.Parse(input, diceSize.Offset, depth);
        var lastOffset = diceSize.Offset;
        int? keepHighest = null, keepLowest = null;
        if (string.IsNullOrEmpty(keep.Error))
        {
            keepValue = IntegerParser.Instance.Parse(input, keep.Offset, depth);
            if (!string.IsNullOrEmpty(keepValue.Error))
                return keepValue;

            if (keepValue.LiteralValue < 1 || keepValue.LiteralValue > diceCount)
                return new ErrorParseTree(keep.Offset,
                    $"Invalid keep count {keepValue.LiteralValue} for dice count {diceCount}");

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
            lastOffset, diceCount, diceSign, diceSize.LiteralValue, keepHighest, keepLowest);
    }
}
