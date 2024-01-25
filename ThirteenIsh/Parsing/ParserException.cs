namespace ThirteenIsh.Parsing;

internal class ParserException : Exception
{
    public ParserException()
    {
    }

    public ParserException(string message) : base(message)
    {
    }

    public ParserException(string message, Exception innerEx) : base(message, innerEx)
    {
    }

    public int Offset { get; init; }
}
