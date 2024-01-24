using System.Diagnostics;

namespace ThirteenIsh.Parsing;

[DebuggerDisplay("({Lhs} {Op} {Rhs})")]
internal sealed class BinaryOperationParseTree(int offset, ParseTreeBase lhs, ParseTreeBase rhs,
    char op) : ParseTreeBase(offset)
{
    public ParseTreeBase Lhs => lhs;
    public ParseTreeBase Rhs => rhs;
    public char Op => op;

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

    public override ParseTreeBase InsertBinaryOperation(ParseTreeBase insertOperand, char insertOperator)
    {
        switch (insertOperator)
        {
            case '+' or '-' when op is '+' or '-':
            case '*' or '/' when op is '*' or '/':
                var newLhs = lhs.InsertBinaryOperation(insertOperand, insertOperator);
                return new BinaryOperationParseTree(Offset, newLhs, rhs, op);

            default:
                return base.InsertBinaryOperation(insertOperand, insertOperator);
        }
    }

    public override string ToString() => $"{lhs} {op} {rhs}";
}
