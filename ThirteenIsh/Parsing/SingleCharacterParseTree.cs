using System.Diagnostics;

namespace ThirteenIsh.Parsing;

/// <summary>
/// Doesn't evaluate anything -- needed only as an intermediate step to parse
/// and check operator characters etc
/// </summary>
[DebuggerDisplay("{Operator}")]
internal sealed class SingleCharacterParseTree(int offset, char ch) : ParseTreeBase(offset)
{
    public override char Operator => ch;

    public override string ToString() => $"{ch}";
}
