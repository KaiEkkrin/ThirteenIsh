namespace ThirteenIsh.Parsing;

/// <summary>
/// Parses a sub-expression enclosed by parentheses (...)
/// </summary>
internal sealed class ParenthesisedExpressionParser : ParserBase
{
    public static readonly ParenthesisedExpressionParser Instance = new();

    private static readonly SingleCharacterParser OpenBracketsParser = new(nameof(ParenthesisedExpressionParser), '(');
    private static readonly SingleCharacterParser CloseBracketsParser = new(nameof(ParenthesisedExpressionParser), ')');

    public override ParseTreeBase Parse(string input, int offset, int depth)
    {
        CheckMaxDepth(offset, ref depth);

        // Parse the (
        var ob = OpenBracketsParser.Parse(input, offset, depth);
        if (!string.IsNullOrEmpty(ob.ParseError)) return ob;

        // Parse the inner expression
        var inner = MultiCaseParser.AddSubMulDivDiceRollOrIntegerParser.Parse(input, ob.Offset, depth);
        if (!string.IsNullOrEmpty(inner.ParseError)) return inner;

        // Parse the )
        var cb = CloseBracketsParser.Parse(input, inner.Offset, depth);
        if (!string.IsNullOrEmpty(cb.ParseError)) return cb;

        return new ParenthesisedExpressionParseTree(cb.Offset, inner);
    }
}
