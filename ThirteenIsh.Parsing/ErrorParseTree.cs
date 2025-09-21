namespace ThirteenIsh.Parsing;

internal sealed class ErrorParseTree(int offset, string error) : ParseTreeBase(offset)
{
    public override string? Error => $"At {Offset} : {error}";
}
