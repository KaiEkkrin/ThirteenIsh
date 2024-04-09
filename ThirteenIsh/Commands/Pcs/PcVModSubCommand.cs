using Discord.WebSocket;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal class PcVModSubCommand() : PcVSubCommandBase("vmod", "Adds to or subtracts from a variable value,",
    "The variable name to change.", "A number or dice expression to change it by.")
{
    protected override EditVariableOperationBase CreateEditOperation(SqlDataService dataService, SocketSlashCommand command,
        string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random)
    {
        return new ModVariableOperation(dataService, command, counterNamePart, parseTree, random);
    }
}
