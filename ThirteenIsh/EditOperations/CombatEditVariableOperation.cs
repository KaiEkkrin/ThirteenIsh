using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Results;

namespace ThirteenIsh.EditOperations;

internal sealed class CombatEditVariableOperation(IEditVariableSubOperation subOperation)
    : SyncEditOperation<CombatEditVariableResult, CombatantResult>
{
    public override EditResult<CombatEditVariableResult> DoEdit(DataContext context, CombatantResult param)
    {
        var gameSystem = GameSystem.Get(param.Adventure.GameSystem);
        var characterSystem = gameSystem.GetCharacterSystem(param.Combatant.CharacterType, param.Character.CharacterSystemName);
        return subOperation.DoEdit(param.Character, characterSystem, gameSystem, out var gameCounter, out var working,
            out var errorMessage)
            ? new EditResult<CombatEditVariableResult>(new CombatEditVariableResult(param, gameCounter, gameSystem, working))
            : CreateError(errorMessage);
    }
}

internal record CombatEditVariableResult(CombatantResult CombatantResult, GameCounter GameCounter,
    GameSystem GameSystem, string Working);

