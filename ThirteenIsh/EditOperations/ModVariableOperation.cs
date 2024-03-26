using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.EditOperations;

internal sealed class ModVariableOperation(SocketInteraction interaction, GameCounter counter, ParseTreeBase parseTree,
    IRandomWrapper random)
    : EditVariableOperationBase(interaction)
{
    protected override MessageEditResult<EditVariableResult> DoEditInternal(Adventure adventure, Adventurer adventurer)
    {
        var currentValue = counter.GetVariableValue(adventurer)
            ?? counter.GetStartingValue(adventurer.Sheet)
            ?? throw new InvalidOperationException($"Variable {counter.Name} has no current or starting value");

        var modParseTree = new BinaryOperationParseTree(0,
            new IntegerParseTree(0, currentValue, counter.Name),
            parseTree,
            '+');

        var newValue = modParseTree.Evaluate(random, out var working);
        counter.SetVariableClamped(newValue, adventurer);
        return new MessageEditResult<EditVariableResult>(new EditVariableResult(adventure, working));
    }
}
