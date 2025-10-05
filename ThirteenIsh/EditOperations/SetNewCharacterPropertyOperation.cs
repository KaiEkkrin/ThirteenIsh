using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;

namespace ThirteenIsh.EditOperations;

/// <summary>
/// Operation to set a character property when adding a new character.
/// (This is separate from SetCharacterPropertyOperation so that we can re-apply the set-on-add
/// properties.)
/// </summary>
internal sealed class SetNewCharacterPropertyOperation(CharacterSystem characterSystem,
    GamePropertyBase property, string newValue)
    : SyncEditOperation<Character, Character>
{
    public override EditResult<Character> DoEdit(DataContext context, Character character)
    {
        if (!property.TryEditCharacterProperty(newValue, character, out var errorMessage))
        {
            return CreateError(errorMessage);
        }

        characterSystem.SetNewCharacterStartingValues(character);
        return new EditResult<Character>(character);
    }
}
