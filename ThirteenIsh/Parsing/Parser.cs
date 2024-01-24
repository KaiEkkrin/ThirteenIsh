namespace ThirteenIsh.Parsing;

/// <summary>
/// This wrapper exists to provide the correct entry point to the parser subsystem
/// </summary>
internal static class Parser
{
    public static ParseTreeBase Parse(string input) => MultiCaseParser.AddSubMulDivDiceRollOrIntegerParser.Parse(input, 0);
}
