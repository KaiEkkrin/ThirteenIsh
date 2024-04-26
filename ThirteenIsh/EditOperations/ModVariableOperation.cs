using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.EditOperations;

internal sealed class ModVariableOperation(string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random)
    : EditVariableOperationBase()
{
    protected override EditResult<EditVariableResult> DoEditInternal(Adventurer adventurer,
        CharacterSystem characterSystem, GameSystem gameSystem)
    {
        var counter = characterSystem.FindCounter(counterNamePart, c => c.Options.HasFlag(GameCounterOptions.HasVariable));
        if (counter == null)
            return CreateError($"'{counterNamePart}' does not uniquely match a variable name.");

        var currentValue = counter.GetVariableValue(adventurer)
            ?? counter.GetStartingValue(adventurer.Sheet)
            ?? throw new InvalidOperationException($"Variable {counter.Name} has no current or starting value");

        var modParseTree = new BinaryOperationParseTree(0,
            new IntegerParseTree(0, currentValue, counter.Name),
            parseTree,
            '+');

        var newValue = modParseTree.Evaluate(random, out var working);
        counter.SetVariableClamped(newValue, adventurer);
        return new EditResult<EditVariableResult>(new EditVariableResult(adventurer, counter, gameSystem,
            working));
    }
}
