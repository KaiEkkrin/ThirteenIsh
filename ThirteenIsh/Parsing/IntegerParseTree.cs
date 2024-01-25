using System.Diagnostics;

namespace ThirteenIsh.Parsing;

[DebuggerDisplay("{LiteralValue}")]
internal sealed class IntegerParseTree(int offset, int value) : ParseTreeBase(offset)
{
    public override int LiteralValue => value;
    
    public override int Evaluate(out string working)
    {
        working = $"{value}";
        return value;
    }

    public override string ToString() => $"{value}";
}
