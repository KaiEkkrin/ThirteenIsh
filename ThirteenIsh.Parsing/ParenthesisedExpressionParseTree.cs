namespace ThirteenIsh.Parsing;

/// <summary>
/// This might seem trivial but is in fact necessary to stop InsertBinaryOperation
/// from unpacking it and messing up the order of operations (and also to track the correct offset.)
/// </summary>
[DebuggerDisplay("({Inner})")]
internal sealed class ParenthesisedExpressionParseTree(int offset, ParseTreeBase inner) : ParseTreeBase(offset)
{
    public ParseTreeBase Inner => inner;

    public override int Evaluate(IRandomWrapper random, out string working)
    {
        var result = inner.Evaluate(random, out var innerWorking);
        working = $"({innerWorking})";
        return result;
    }

    public override string ToString() => $"({inner})";
}
