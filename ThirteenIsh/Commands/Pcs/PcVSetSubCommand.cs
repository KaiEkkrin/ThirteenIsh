using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcVSetSubCommand() : PcVSubCommandBase("vset", "Sets a variable value.",
    "The variable name to set.", "A number or dice expression to set it to.")
{
    protected override PcEditVariableOperation CreateEditOperation(
        string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random)
    {
        return new PcEditVariableOperation(new SetVariableSubOperation(counterNamePart, parseTree, random));
    }
}
