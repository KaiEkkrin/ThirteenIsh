namespace ThirteenIsh.Parsing;

/// <summary>
/// The basis of our parse trees.
/// </summary>
internal abstract class ParseTreeBase(int offset)
{
    /// <summary>
    /// If there is a parse error, returns it. Otherwise null.
    /// </summary>
    public virtual string? Error => null;

    /// <summary>
    /// The offset after parsing. For a successful parse this should be the string length.
    /// </summary>
    public int Offset => offset;

    /// <summary>
    /// Evaluates the parse tree and returns the result. Fills the out parameter with
    /// a description of how the result was calculated.
    /// </summary>
    public virtual int Evaluate(out string working)
    {
        throw new InvalidOperationException($"Cannot evaluate: {Error}");
    }
}
