namespace ThirteenIsh.Parsing;

/// <summary>
/// Parses a sub-expression enclosed by parentheses (...)
/// </summary>
internal sealed class ParenthesisedExpressionParser : ParserBase
{
    public static readonly ParenthesisedExpressionParser Instance = new();

    public override ParseTreeBase Parse(string input, int offset)
    {
        // Parse the (
        if (offset >= input.Length)
            return new ErrorParseTree(offset,
                $"ParenthesisedExpressionParser: expected '(', got end of input");

        if (input[offset] != '(')
            return new ErrorParseTree(offset,
                $"ParenthesisedExpressionParser: expected '(', got '{input[offset]}'");

        // Parse the inner expression
        var inner = MultiCaseParser.AddSubMulDivDiceRollOrIntegerParser.Parse(input, offset + 1);
        if (!string.IsNullOrEmpty(inner.Error)) return inner;

        // Parse the )
        if (inner.Offset >= input.Length)
            return new ErrorParseTree(inner.Offset,
                $"ParenthesisedExpressionParser: expected ')', got end of input");

        if (input[inner.Offset] != ')')
            return new ErrorParseTree(inner.Offset,
                $"ParenthesisedExpressionParser: expected '(', got '{input[inner.Offset]}'");

        return new ParenthesisedExpressionParseTree(inner.Offset + 1, inner);
    }
}
