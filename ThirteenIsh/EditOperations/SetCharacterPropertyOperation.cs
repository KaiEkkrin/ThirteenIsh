using ThirteenIsh.Game;

namespace ThirteenIsh.EditOperations;

internal sealed class SetCharacterPropertyOperation(GamePropertyBase property, string newValue)
    : SyncEditOperation<ResultOrMessage<Entities.Character>, Entities.Character, MessageEditResult<Entities.Character>>
{
    public override EditResult<ResultOrMessage<Entities.Character>> CreateError(string message)
    {
        return new MessageEditResult<Entities.Character>(null, message);
    }

    public override MessageEditResult<Entities.Character> DoEdit(Entities.Character character)
    {
        if (!property.TryEditCharacterProperty(newValue, character.Sheet, out var errorMessage))
        {
            return new MessageEditResult<Entities.Character>(null, errorMessage);
        }

        return new MessageEditResult<Entities.Character>(character);
    }
}
