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
            input = input.Trim();
            var parseTree = MultiCaseParser.AddSubMulDivDiceRollOrIntegerParser.Parse(input, 0, 0);

            // Make sure we've managed to consume the whole input, or this is a fail :)
            return parseTree switch
            {
                { Error.Length: 0 } => parseTree,
                _ when parseTree.Offset == input.Length => parseTree,
                _ when parseTree.Offset > input.Length => throw new InvalidOperationException(
                    $"Somehow parsed beyond the end of the input. Offset = {parseTree.Offset}, input = '{input}'"),
                _ => new ErrorParseTree(parseTree.Offset, "Unrecognised input at end of string")
            };
        }
        catch (ParserException ex)
        {
            return new ErrorParseTree(ex.Offset, ex.Message);
        }
    }
}
