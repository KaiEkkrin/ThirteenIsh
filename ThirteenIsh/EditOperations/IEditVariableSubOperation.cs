using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;

namespace ThirteenIsh.EditOperations;

internal interface IEditVariableSubOperation
{
    bool DoEdit(ITrackedCharacter character, CharacterSystem characterSystem, GameSystem gameSystem,
        [MaybeNullWhen(false)] out GameCounter gameCounter,
        [MaybeNullWhen(false)] out string working,
        [MaybeNullWhen(true)] out string errorMessage);
}
