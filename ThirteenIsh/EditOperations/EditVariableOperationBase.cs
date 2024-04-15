using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.EditOperations;

internal abstract class EditVariableOperationBase(SqlDataService dataService, SocketInteraction interaction)
    : EditOperation<ResultOrMessage<EditVariableResult>, Adventure, MessageEditResult<EditVariableResult>>
{
    public sealed override async Task<MessageEditResult<EditVariableResult>> DoEditAsync(DataContext context,
        Adventure adventure, CancellationToken cancellationToken = default)
    {
        var adventurer = await dataService.GetAdventurerAsync(adventure, interaction.User.Id, cancellationToken);
        if (adventurer is null)
            return new MessageEditResult<EditVariableResult>(null, "You have not joined the current adventure.");

        var gameSystem = GameSystem.Get(adventure.GameSystem);
        var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);
        return DoEditInternal(adventure, adventurer, characterSystem, gameSystem);
    }

    protected abstract MessageEditResult<EditVariableResult> DoEditInternal(Adventure adventure, Adventurer adventurer,
        CharacterSystem characterSystem, GameSystem gameSystem);
}

internal record EditVariableResult(Adventure Adventure, Adventurer Adventurer, GameCounter GameCounter,
    GameSystem GameSystem, string Working);

