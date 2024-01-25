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
    /// The literal integer value that was parsed (only for IntegerParseTree.)
    /// </summary>
    public virtual int LiteralValue => throw new NotSupportedException();

    /// <summary>
    /// The offset after parsing. For a successful parse this should be the string length.
    /// </summary>
    public int Offset => offset;

    /// <summary>
    /// The parsed operator character (only for SingleCharacterParseTree.)
    /// </summary>
    public virtual char Operator => throw new NotSupportedException();

    /// <summary>
    /// Evaluates the parse tree and returns the result. Fills the out parameter with
    /// a description of how the result was calculated.
    /// </summary>
    public virtual int Evaluate(out string working)
    {
        throw new InvalidOperationException($"Cannot evaluate: {Error}");
    }

    /// <summary>
    /// Inserts a binary operation into this parse tree.
    /// This method is necessary because chains of subtractions or divisions will naturally
    /// parse with the wrong associativity -- by calling this the appropriate overload can
    /// flip that associativity around.
    /// </summary>
    public virtual ParseTreeBase InsertBinaryOperation(ParseTreeBase insertOperand, char insertOperator)
    {
        return new BinaryOperationParseTree(Offset, insertOperand, this, insertOperator);
    }
}
