﻿namespace ThirteenIsh.Parsing;

/// <summary>
/// Parses an add or subtract sub-expression.
/// </summary>
internal sealed class AddSubtractParser : ParserBase
{
    public static readonly AddSubtractParser Instance = new();

    private static readonly SingleCharacterParser OpParser = new(nameof(AddSubtractParser), '+', '-');

    public override ParseTreeBase Parse(string input, int offset, int depth)
    {
        CheckMaxDepth(offset, ref depth);

        // Parse the left operand
        var lhs = MultiCaseParser.MulDivDiceRollOrIntegerParser.Parse(input, offset, depth);
        if (!string.IsNullOrEmpty(lhs.Error)) return lhs;

        // Parse the '+' or '-'
        var op = OpParser.Parse(input, lhs.Offset, depth);
        if (!string.IsNullOrEmpty(op.Error)) return op;

        // Parse the right operand
        var rhs = MultiCaseParser.AddSubMulDivDiceRollOrIntegerParser.Parse(input, op.Offset, depth);
        if (!string.IsNullOrEmpty(rhs.Error)) return rhs;

        return rhs.InsertBinaryOperation(lhs, op.Operator);
    }
}
