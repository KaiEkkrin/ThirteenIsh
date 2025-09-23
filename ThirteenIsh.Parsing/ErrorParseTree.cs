namespace ThirteenIsh.Parsing;

internal sealed class ErrorParseTree(int offset, string error) : ParseTreeBase(offset)
{
    public override string? ParseError => $"At {Offset} : {error}";
}
