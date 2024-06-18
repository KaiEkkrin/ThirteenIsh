using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.EditOperations;

internal sealed class ModVariableSubOperation(string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random)
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

        var currentValue = gameCounter.GetVariableValue(character)
            ?? throw new InvalidOperationException($"Variable {gameCounter.Name} has no value");

        var modParseTree = new BinaryOperationParseTree(0,
            new IntegerParseTree(0, currentValue, gameCounter.Name),
            parseTree,
            '+');

        var newValue = modParseTree.Evaluate(random, out working);
        gameCounter.SetVariableClamped(newValue, character);
        errorMessage = null;
        return true;
    }
}
