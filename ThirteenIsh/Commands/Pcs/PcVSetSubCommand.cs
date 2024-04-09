using Discord.WebSocket;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcVSetSubCommand() : PcVSubCommandBase("vset", "Sets a variable value.",
    "The variable name to set.", "A number or dice expression to set it to.")
{
    protected override EditVariableOperationBase CreateEditOperation(SqlDataService dataService, SocketSlashCommand command,
        string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random)
    {
        return new SetVariableOperation(dataService, command, counterNamePart, parseTree, random);
    }
}
