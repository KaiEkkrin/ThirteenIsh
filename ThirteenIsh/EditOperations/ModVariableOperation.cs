using Discord.WebSocket;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.EditOperations;

internal sealed class ModVariableOperation(SqlDataService dataService, SocketInteraction interaction, string counterNamePart,
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

        var currentValue = counter.GetVariableValue(adventurer)
            ?? counter.GetStartingValue(adventurer.Sheet)
            ?? throw new InvalidOperationException($"Variable {counter.Name} has no current or starting value");

        var modParseTree = new BinaryOperationParseTree(0,
            new IntegerParseTree(0, currentValue, counter.Name),
            parseTree,
            '+');

        var newValue = modParseTree.Evaluate(random, out var working);
        counter.SetVariableClamped(newValue, adventurer);
        return new MessageEditResult<EditVariableResult>(new EditVariableResult(adventure, adventurer, counter, gameSystem,
            working));
    }
}
