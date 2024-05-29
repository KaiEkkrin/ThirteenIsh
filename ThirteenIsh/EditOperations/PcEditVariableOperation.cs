using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;

namespace ThirteenIsh.EditOperations;

internal sealed class PcEditVariableOperation(IEditVariableSubOperation subOperation)
    : SyncEditOperation<PcEditVariableResult, Adventurer>
{
    public override EditResult<PcEditVariableResult> DoEdit(DataContext context, Adventurer adventurer)
    {
        var gameSystem = GameSystem.Get(adventurer.Adventure.GameSystem);
        var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);
        return subOperation.DoEdit(adventurer, characterSystem, gameSystem, out var gameCounter, out var working,
            out var errorMessage)
            ? new EditResult<PcEditVariableResult>(new PcEditVariableResult(adventurer, gameCounter, gameSystem, working))
            : CreateError(errorMessage);
    }
}

internal record PcEditVariableResult(Adventurer Adventurer, GameCounter GameCounter,
    GameSystem GameSystem, string Working);

