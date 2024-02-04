using System.Diagnostics;

namespace ThirteenIsh.Parsing;

[DebuggerDisplay("{LiteralValue}")]
internal sealed class IntegerParseTree(int offset, int value, string? name = null) : ParseTreeBase(offset)
{
    public override int LiteralValue => value;
    
    public override int Evaluate(out string working)
    {
        working = string.IsNullOrEmpty(name)
            ? $"{value}"
            : $"{value} [{name}]";

        return value;
    }

    public override string ToString() => $"{value}";
}
