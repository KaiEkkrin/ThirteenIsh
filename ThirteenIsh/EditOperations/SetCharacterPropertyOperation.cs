using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;

namespace ThirteenIsh.EditOperations;

internal sealed class SetCharacterPropertyOperation(GamePropertyBase property, string newValue)
    : SyncEditOperation<ResultOrMessage<Character>, Character, MessageEditResult<Character>>
{
    public override EditResult<ResultOrMessage<Character>> CreateError(string message)
    {
        return new MessageEditResult<Character>(null, message);
    }

    public override MessageEditResult<Character> DoEdit(DataContext context, Character character)
    {
        if (!property.TryEditCharacterProperty(newValue, character.Sheet, out var errorMessage))
        {
            return new MessageEditResult<Character>(null, errorMessage);
        }

        return new MessageEditResult<Character>(character);
    }
}
