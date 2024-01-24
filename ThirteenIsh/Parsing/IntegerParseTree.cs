using System.Diagnostics;

namespace ThirteenIsh.Parsing;

[DebuggerDisplay("{Value}")]
internal sealed class IntegerParseTree(int offset, int value) : ParseTreeBase(offset)
{
    public int Value => value;
    
    public override int Evaluate(out string working)
    {
        working = $"{value}";
        return value;
    }

    public override string ToString() => $"{value}";
}
