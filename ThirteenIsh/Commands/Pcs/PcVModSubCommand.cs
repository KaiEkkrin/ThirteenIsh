using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Pcs;

internal class PcVModSubCommand(bool asGm) : PcVSubCommandBase(asGm, "vmod", "Adds to or subtracts from a variable value,",
    "The variable name to change.", "A number or dice expression to change it by.")
{
    protected override PcEditVariableOperation CreateEditOperation(
        string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random)
    {
        return new PcEditVariableOperation(new ModVariableSubOperation(counterNamePart, parseTree, random));
    }
}
