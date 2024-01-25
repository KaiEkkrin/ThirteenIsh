namespace ThirteenIsh.Parsing;

/// <summary>
/// This wrapper exists to provide the correct entry point to the parser subsystem
/// </summary>
internal static class Parser
{
    public static ParseTreeBase Parse(string input)
    {
        try
        {
            return MultiCaseParser.AddSubMulDivDiceRollOrIntegerParser.Parse(input.Trim(), 0, 0);
        }
        catch (ParserException ex)
        {
            return new ErrorParseTree(ex.Offset, ex.Message);
        }
    }
}
