using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.EditOperations;

internal sealed class SetVariableOperation(string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random)
    : EditVariableOperationBase()
{
    protected override EditResult<EditVariableResult> DoEditInternal(Adventurer adventurer,
        CharacterSystem characterSystem, GameSystem gameSystem)
    {
        var counter = characterSystem.FindCounter(adventurer.Sheet, counterNamePart,
            c => c.Options.HasFlag(GameCounterOptions.HasVariable));

        if (counter == null)
            return CreateError($"'{counterNamePart}' does not uniquely match a variable name.");

        var newValue = parseTree.Evaluate(random, out var working);
        if (!counter.TrySetVariable(newValue, adventurer, out var errorMessage))
            return CreateError(errorMessage);

        return new EditResult<EditVariableResult>(new EditVariableResult(adventurer, counter, gameSystem, working));
    }
}
