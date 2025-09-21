namespace ThirteenIsh.Parsing;

public abstract class ParserBase
{
    public const int MaxDepth = 200;

    public abstract ParseTreeBase Parse(string input, int offset, int depth);

    /// <summary>
    /// Call this at the top of every Parse implementation.
    /// Here to prevent log input from overflowing the stack and crashing the process,
    /// as well as avoiding slow operations since ParseTreeBase.InsertBinaryOperation's usage is O(n^2)
    /// </summary>
    protected static void CheckMaxDepth(int offset, ref int depth)
    {
        // If I hit the max depth I want to just bail out right away rather than checking
        // other branches of the parse tree anyway
        if (++depth > MaxDepth) throw new ParserException("Input is too large.") { Offset = offset };
    }
}
