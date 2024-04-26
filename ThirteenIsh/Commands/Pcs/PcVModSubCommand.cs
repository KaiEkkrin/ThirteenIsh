using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Pcs;

internal class PcVModSubCommand() : PcVSubCommandBase("vmod", "Adds to or subtracts from a variable value,",
    "The variable name to change.", "A number or dice expression to change it by.")
{
    protected override EditVariableOperationBase CreateEditOperation(
        string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random)
    {
        return new ModVariableOperation(counterNamePart, parseTree, random);
    }
}
