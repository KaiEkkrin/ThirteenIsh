namespace ThirteenIsh.Parsing;

/// <summary>
/// Parses an add or subtract sub-expression.
/// </summary>
internal sealed class AddSubtractParser : ParserBase
{
    public static readonly AddSubtractParser Instance = new();

    public override ParseTreeBase Parse(string input, int offset)
    {
        // Parse the left operand
        var lhs = MultiCaseParser.MulDivDiceRollOrIntegerParser.Parse(input, offset);
        if (!string.IsNullOrEmpty(lhs.Error)) return lhs;

        // Parse the '+' or '-'
        if (lhs.Offset >= input.Length)
            return new ErrorParseTree(lhs.Offset,
                "AddSubtractParser: expected '+' or '-', got end of input");

        var op = input[lhs.Offset];
        if (op is not ('+' or '-'))
            return new ErrorParseTree(lhs.Offset,
                $"MultiplyDivideParser: expected '+' or '-', got '{input[lhs.Offset]}'");

        // Parse the right operand
        // TODO I potentially need to flip the parse tree around now to get the
        // correct associativity, don't I...
        var rhs = MultiCaseParser.AddSubMulDivDiceRollOrIntegerParser.Parse(input, lhs.Offset + 1);
        if (!string.IsNullOrEmpty(rhs.Error)) return rhs;

        return rhs.InsertBinaryOperation(lhs, op);
    }
}
