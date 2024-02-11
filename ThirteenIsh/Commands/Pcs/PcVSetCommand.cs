using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcVSetCommand() : PcVCommandBase("vset", "Sets a variable value.",
    "The variable name to set.", "A number or dice expression to set it to.")
{
    protected override VCommandEditOperation CreateEditOperation(SocketSlashCommand command,
        GameCounter counter, ParseTreeBase parseTree, IRandomWrapper random)
    {
        return new EditOperation(command, counter, parseTree, random);
    }

    private sealed class EditOperation(SocketSlashCommand command, GameCounter counter, ParseTreeBase parseTree,
        IRandomWrapper random)
        : VCommandEditOperation(command)
    {
        protected override MessageEditResult<VCommandResult> DoEditInternal(Adventure adventure, Adventurer adventurer)
        {
            var newValue = parseTree.Evaluate(random, out var working);
            if (!counter.TrySetVariable(newValue, adventurer, out var errorMessage))
                return new MessageEditResult<VCommandResult>(null, errorMessage);

            return new MessageEditResult<VCommandResult>(new VCommandResult(adventure, working));
        }
    }
}
