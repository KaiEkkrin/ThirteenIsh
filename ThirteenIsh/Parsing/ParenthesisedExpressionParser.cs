﻿namespace ThirteenIsh.Parsing;

/// <summary>
/// Parses a sub-expression enclosed by parentheses (...)
/// </summary>
internal sealed class ParenthesisedExpressionParser : ParserBase
{
    public static readonly ParenthesisedExpressionParser Instance = new();

    private static readonly SingleCharacterParser OpenBracketsParser = new(nameof(ParenthesisedExpressionParser), '(');
    private static readonly SingleCharacterParser CloseBracketsParser = new(nameof(ParenthesisedExpressionParser), ')');

    public override ParseTreeBase Parse(string input, int offset)
    {
        // Parse the (
        var ob = OpenBracketsParser.Parse(input, offset);
        if (!string.IsNullOrEmpty(ob.Error)) return ob;

        // Parse the inner expression
        var inner = MultiCaseParser.AddSubMulDivDiceRollOrIntegerParser.Parse(input, ob.Offset);
        if (!string.IsNullOrEmpty(inner.Error)) return inner;

        // Parse the )
        var cb = CloseBracketsParser.Parse(input, inner.Offset);
        if (!string.IsNullOrEmpty(cb.Error)) return cb;

        return new ParenthesisedExpressionParseTree(cb.Offset, inner);
    }
}
