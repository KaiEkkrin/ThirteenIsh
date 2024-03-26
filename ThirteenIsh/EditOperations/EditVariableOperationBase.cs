using Discord.WebSocket;
using ThirteenIsh.Entities;

namespace ThirteenIsh.EditOperations;

internal abstract class EditVariableOperationBase(SocketInteraction interaction)
    : SyncEditOperation<ResultOrMessage<EditVariableResult>, Guild, MessageEditResult<EditVariableResult>>
{
    public sealed override MessageEditResult<EditVariableResult> DoEdit(Guild guild)
    {
        if (guild.CurrentAdventure is not { } currentAdventure)
            return new MessageEditResult<EditVariableResult>(null, "There is no current adventure in this guild.");

        if (!currentAdventure.Adventurers.TryGetValue(interaction.User.Id, out var adventurer))
            return new MessageEditResult<EditVariableResult>(null, "You have not joined the current adventure.");

        return DoEditInternal(currentAdventure, adventurer);
    }

    protected abstract MessageEditResult<EditVariableResult> DoEditInternal(Adventure adventure, Adventurer adventurer);
}

internal record EditVariableResult(Adventure Adventure, string Working);

