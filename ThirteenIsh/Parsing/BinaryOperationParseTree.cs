namespace ThirteenIsh.Parsing;

internal sealed class BinaryOperationParseTree(int offset, ParseTreeBase lhs, ParseTreeBase rhs,
    char op) : ParseTreeBase(offset)
{
    public override int Evaluate(out string working)
    {
        var left = lhs.Evaluate(out var lhsWorking);
        var right = rhs.Evaluate(out var rhsWorking);
        working = $"{lhsWorking} {op} {rhsWorking}";
        return op switch
        {
            '+' => left + right,
            '-' => left - right,
            '*' => left * right,
            '/' => left / right,
            _ => throw new InvalidOperationException($"Unrecognised binary operator: {op}")
        };
    }

    public override string ToString() => $"{lhs} {op} {rhs}";
}
