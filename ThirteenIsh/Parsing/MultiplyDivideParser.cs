namespace ThirteenIsh.Parsing;

/// <summary>
/// Parses a multiply or divide sub-expression.
/// </summary>
internal sealed class MultiplyDivideParser : ParserBase
{
    public static readonly MultiplyDivideParser Instance = new();

    public override ParseTreeBase Parse(string input, int offset)
    {
        // Parse the left operand
        var lhs = MultiCaseParser.DiceRollOrIntegerParser.Parse(input, offset);
        if (!string.IsNullOrEmpty(lhs.Error)) return lhs;

        // Parse the '*' or '/'
        if (lhs.Offset >= input.Length)
            return new ErrorParseTree(lhs.Offset,
                $"MultiplyDivideParser: expected '*' or '/', got end of input");

        var op = input[lhs.Offset];
        if (op is not ('*' or '/'))
            return new ErrorParseTree(lhs.Offset,
                $"MultiplyDivideParser: expected '*' or '/', got '{input[lhs.Offset]}'");

        // Parse the right operand
        // TODO I potentially need to flip the parse tree around now to get the
        // correct associativity, don't I...
        var rhs = MultiCaseParser.MulDivDiceRollOrIntegerParser.Parse(input, lhs.Offset + 1);
        if (!string.IsNullOrEmpty(rhs.Error)) return rhs;

        return rhs.InsertBinaryOperation(lhs, op);
    }
}
