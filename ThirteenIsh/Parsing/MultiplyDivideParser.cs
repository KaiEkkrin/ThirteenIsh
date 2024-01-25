namespace ThirteenIsh.Parsing;

/// <summary>
/// Parses a multiply or divide sub-expression.
/// </summary>
internal sealed class MultiplyDivideParser : ParserBase
{
    public static readonly MultiplyDivideParser Instance = new();

    private static readonly SingleCharacterParser OpParser = new(nameof(MultiplyDivideParser), '*', '/');

    public override ParseTreeBase Parse(string input, int offset, int depth)
    {
        CheckMaxDepth(offset, ref depth);

        // Parse the left operand
        var lhs = MultiCaseParser.DiceRollOrIntegerParser.Parse(input, offset, depth);
        if (!string.IsNullOrEmpty(lhs.Error)) return lhs;

        // Parse the '*' or '/'
        var op = OpParser.Parse(input, lhs.Offset, depth);
        if (!string.IsNullOrEmpty(op.Error)) return op;

        // Parse the right operand
        var rhs = MultiCaseParser.MulDivDiceRollOrIntegerParser.Parse(input, op.Offset, depth);
        if (!string.IsNullOrEmpty(rhs.Error)) return rhs;

        return rhs.InsertBinaryOperation(lhs, op.Operator);
    }
}
