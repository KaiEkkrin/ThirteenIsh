using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Combat;

internal sealed class CombatVModSubCommand() : CombatVSubCommandBase("vmod", "Adds to or subtracts from a variable value,",
    "The variable name to change.", "A number or dice expression to change it by.")
{
    protected override CombatEditVariableOperation CreateEditOperation(string counterNamePart, ParseTreeBase parseTree,
        IRandomWrapper random)
    {
        return new CombatEditVariableOperation(new ModVariableSubOperation(counterNamePart, parseTree, random));
    }
}
