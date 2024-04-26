using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;

namespace ThirteenIsh.EditOperations;

internal abstract class EditVariableOperationBase()
    : SyncEditOperation<EditVariableResult, Adventurer>
{
    public sealed override EditResult<EditVariableResult> DoEdit(DataContext context, Adventurer adventurer)
    {
        var gameSystem = GameSystem.Get(adventurer.Adventure.GameSystem);
        var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);
        return DoEditInternal(adventurer, characterSystem, gameSystem);
    }

    protected abstract EditResult<EditVariableResult> DoEditInternal(Adventurer adventurer,
        CharacterSystem characterSystem, GameSystem gameSystem);
}

internal record EditVariableResult(Adventurer Adventurer, GameCounter GameCounter,
    GameSystem GameSystem, string Working);

