namespace ThirteenIsh.Parsing;

internal sealed class SingleCharacterParser(string context, params char[] validCharacters) : ParserBase
{
    private string Expectations => string.Join(" or ", validCharacters.Select(c => $"'{c}'"));

    public override ParseTreeBase Parse(string input, int offset)
    {
        if (offset >= input.Length)
            return new ErrorParseTree(offset, $"{context}: expected {Expectations}, got end of input");

        var ch = input[offset];
        if (!validCharacters.Contains(ch))
            return new ErrorParseTree(offset, $"{context}: expected {Expectations}, got '{ch}'");

        return new SingleCharacterParseTree(offset + 1, ch);
    }
}
