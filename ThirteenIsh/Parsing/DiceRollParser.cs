namespace ThirteenIsh.Parsing;

/// <summary>
/// Parses a dice roll sub-expression.
/// </summary>
internal sealed class DiceRollParser : ParserBase
{
    public static readonly DiceRollParser Instance = new();

    public override ParseTreeBase Parse(string input, int offset)
    {
        if (offset >= input.Length)
            return new ErrorParseTree(offset, $"DiceRollParser: expected dice roll, got end of input");

        // Parse the dice count
        var diceCount = IntegerParser.Instance.Parse(input, offset);
        if (!string.IsNullOrEmpty(diceCount.Error)) return diceCount;
        if (diceCount is not IntegerParseTree integerDiceCount)
            throw new InvalidOperationException($"Parsed dice count as a {diceCount.GetType()}");

        // Parse the "d"
        if (diceCount.Offset >= input.Length)
            return new ErrorParseTree(diceCount.Offset,
                $"DiceRollParser: expected 'd', got end of input");

        if (input[diceCount.Offset] != 'd')
            return new ErrorParseTree(diceCount.Offset,
                $"DiceRollParser: expected 'd', got '{input[diceCount.Offset]}'");

        // Parse the dice size
        var diceSize = IntegerParser.Instance.Parse(input, diceCount.Offset + 1);
        if (!string.IsNullOrEmpty(diceSize.Error)) return diceSize;
        if (diceSize is not IntegerParseTree { Value: > 0 } integerDiceSize)
            return new ErrorParseTree(diceCount.Offset + 1,
                $"DiceRollParser: expected dice size 1 or greater, got '{diceSize}'");

        return new DiceRollParseTree(diceSize.Offset, integerDiceCount.Value, integerDiceSize.Value);
    }
}
