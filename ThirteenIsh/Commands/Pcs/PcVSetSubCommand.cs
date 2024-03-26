using Discord.WebSocket;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcVSetSubCommand() : PcVSubCommandBase("vset", "Sets a variable value.",
    "The variable name to set.", "A number or dice expression to set it to.")
{
    protected override EditVariableOperationBase CreateEditOperation(SocketSlashCommand command,
        GameCounter counter, ParseTreeBase parseTree, IRandomWrapper random)
    {
        return new SetVariableOperation(command, counter, parseTree, random);
    }
}
