using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Combat;

internal sealed class CombatVSetSubCommand() : CombatVSubCommandBase("vset", "Sets a variable value.",
    "The variable name to set.", "A number or dice expression to set it to.")
{
    protected override CombatEditVariableOperation CreateEditOperation(string counterNamePart, ParseTreeBase parseTree,
        IRandomWrapper random)
    {
        return new CombatEditVariableOperation(new SetVariableSubOperation(counterNamePart, parseTree, random));
    }
}
