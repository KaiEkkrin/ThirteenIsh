using Discord.WebSocket;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.EditOperations;

internal sealed class SetVariableOperation(SqlDataService dataService, SocketInteraction interaction, string counterNamePart,
    ParseTreeBase parseTree, IRandomWrapper random)
    : EditVariableOperationBase(dataService, interaction)
{
    protected override MessageEditResult<EditVariableResult> DoEditInternal(Adventure adventure, Adventurer adventurer,
        CharacterSystem characterSystem, GameSystem gameSystem)
    {
        var counter = characterSystem.FindCounter(counterNamePart, c => c.Options.HasFlag(GameCounterOptions.HasVariable));
        if (counter == null)
            return new MessageEditResult<EditVariableResult>(null,
                $"'{counterNamePart}' does not uniquely match a variable name.");

        var newValue = parseTree.Evaluate(random, out var working);
        if (!counter.TrySetVariable(newValue, adventurer, out var errorMessage))
            return new MessageEditResult<EditVariableResult>(null, errorMessage);

        return new MessageEditResult<EditVariableResult>(new EditVariableResult(adventure, adventurer, counter, gameSystem,
            working));
    }
}
