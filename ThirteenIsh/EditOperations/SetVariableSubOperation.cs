using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.EditOperations;

internal sealed class SetVariableSubOperation(string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random)
    : IEditVariableSubOperation
{
    public bool DoEdit(ITrackedCharacter character, CharacterSystem characterSystem, GameSystem gameSystem,
        [MaybeNullWhen(false)] out GameCounter gameCounter,
        [MaybeNullWhen(false)] out string working,
        [MaybeNullWhen(true)] out string errorMessage)
    {
        gameCounter = characterSystem.FindCounter(character.Sheet, counterNamePart,
            c => c.Options.HasFlag(GameCounterOptions.HasVariable));

        if (gameCounter == null)
        {
            working = null;
            errorMessage = $"'{counterNamePart}' does not uniquely match a variable name.";
            return false;
        }

        var newValue = parseTree.Evaluate(random, out working);
        return gameCounter.TrySetVariable(newValue, character, out errorMessage);
    }
}
