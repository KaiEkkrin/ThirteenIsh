namespace ThirteenIsh.Parsing;

internal abstract class ParserBase
{
    public abstract ParseTreeBase Parse(string input, int offset);
}
