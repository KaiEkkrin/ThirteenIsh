using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;

namespace ThirteenIsh.EditOperations;

internal sealed class SetCharacterPropertyOperation(GamePropertyBase property, string newValue)
    : SyncEditOperation<Character, Character>
{
    public override EditResult<Character> DoEdit(DataContext context, Character character)
    {
        if (!property.TryEditCharacterProperty(newValue, character, out var errorMessage))
        {
            return CreateError(errorMessage);
        }

        return new EditResult<Character>(character);
    }
}
