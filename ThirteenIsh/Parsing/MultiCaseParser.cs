namespace ThirteenIsh.Parsing;

/// <summary>
/// A parser that chooses the first success out of the given parsers in order.
/// </summary>
internal sealed class MultiCaseParser(params ParserBase[] parsers) : ParserBase
{
    public static readonly MultiCaseParser DiceRollOrIntegerParser =
        new(DiceRollParser.Instance, IntegerParser.Instance);

    public static readonly MultiCaseParser MulDivDiceRollOrIntegerParser =
        new(MultiplyDivideParser.Instance, DiceRollParser.Instance, IntegerParser.Instance);

    public static readonly MultiCaseParser AddSubMulDivDiceRollOrIntegerParser =
        new(AddSubtractParser.Instance, MultiplyDivideParser.Instance, DiceRollParser.Instance, IntegerParser.Instance);

    public override ParseTreeBase Parse(string input, int offset)
    {
        List<ParseTreeBase> errors = new();
        foreach (var parser in parsers)
        {
            var parseTree = parser.Parse(input, offset);
            if (string.IsNullOrEmpty(parseTree.Error)) return parseTree;

            errors.Add(parseTree);
        }

        // If we got here all parsing attempts failed. Return the most plausible error -- that's
        // the one with the highest offset
        return errors.MaxBy(e => e.Offset)
            ?? throw new InvalidOperationException("MultiCaseParser must have at least one parser");
    }
}
