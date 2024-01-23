namespace ThirteenIsh.Parsing;

/// <summary>
/// Parses an integer.
/// </summary>
internal sealed class IntegerParser : ParserBase
{
    public static readonly IntegerParser Instance = new();

    public override ParseTreeBase Parse(string input, int offset)
    {
        var nextOffset = offset;
        while (nextOffset < input.Length && char.IsAsciiDigit(input[nextOffset])) ++nextOffset;

        if (nextOffset == offset)
            return new ErrorParseTree(offset, "IntegerParser: expected digit, " +
                (nextOffset >= input.Length ? "end of input" : $"'{input[nextOffset]}'"));

        return !int.TryParse(input[offset..nextOffset], out var value)
            ? new ErrorParseTree(offset, $"IntegerParser: expected integer, got '{input[offset..nextOffset]}'")
            : new IntegerParseTree(nextOffset, value);
    }
}
