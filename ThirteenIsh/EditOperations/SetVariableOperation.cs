using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.EditOperations;

internal sealed class SetVariableOperation(SocketInteraction interaction, GameCounter counter, ParseTreeBase parseTree,
    IRandomWrapper random)
    : EditVariableOperationBase(interaction)
{
    protected override MessageEditResult<EditVariableResult> DoEditInternal(Adventure adventure, Adventurer adventurer)
    {
        var newValue = parseTree.Evaluate(random, out var working);
        if (!counter.TrySetVariable(newValue, adventurer, out var errorMessage))
            return new MessageEditResult<EditVariableResult>(null, errorMessage);

        return new MessageEditResult<EditVariableResult>(new EditVariableResult(adventure, working));
    }
}
