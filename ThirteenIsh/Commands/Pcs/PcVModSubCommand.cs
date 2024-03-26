using Discord.WebSocket;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Pcs;

internal class PcVModSubCommand() : PcVSubCommandBase("vmod", "Adds to or subtracts from a variable value,",
    "The variable name to change.", "A number or dice expression to change it by.")
{
    protected override EditVariableOperationBase CreateEditOperation(SocketSlashCommand command,
        GameCounter counter, ParseTreeBase parseTree, IRandomWrapper random)
    {
        return new ModVariableOperation(command, counter, parseTree, random);
    }
}
