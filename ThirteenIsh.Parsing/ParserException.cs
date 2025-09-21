namespace ThirteenIsh.Parsing;

/// <summary>
/// Use this (sparingly) for a hard error during parsing, that should be a fail regardless
/// of whether any other branches succeeded
/// </summary>
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
