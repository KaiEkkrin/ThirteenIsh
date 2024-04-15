using Discord.WebSocket;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(ResetAdventurerMessage))]
internal sealed class ResetAdventureMessageHandler(SqlDataService dataService) : MessageHandlerBase<ResetAdventurerMessage>
{
    protected override async Task<bool> HandleInternalAsync(SocketMessageComponent component, string controlId,
        ResetAdventurerMessage message, CancellationToken cancellationToken = default)
    {
        var result = await dataService.EditAdventureAsync(
            message.GuildId, new EditOperation(dataService, message.Name, message.UserId), message.Name, cancellationToken);
        
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            await component.RespondAsync(result.ErrorMessage, ephemeral: true);
            return true;
        }

        if (result.Value is null) throw new InvalidOperationException(nameof(result.Value));

        await CommandUtil.RespondWithAdventurerSummaryAsync(component, result.Value.Adventurer, result.Value.GameSystem,
            new CommandUtil.AdventurerSummaryOptions
            {
                OnlyVariables = true,
                Title = $"Reset adventurer {result.Value.Adventurer.Name}"
            });

        return true;
    }

    private sealed class EditOperation(SqlDataService dataService, string adventureName, ulong userId)
        : EditOperation<ResultOrMessage<EditResult>, Adventure, MessageEditResult<EditResult>>
    {
        public override async Task<MessageEditResult<EditResult>> DoEditAsync(DataContext context, Adventure adventure,
            CancellationToken cancellationToken = default)
        {
            var adventurer = await dataService.GetAdventurerAsync(adventure, userId, cancellationToken);
            if (adventurer == null)
                return new MessageEditResult<EditResult>(null, $"You do not have a character in the adventure '{adventureName}'.");

            var gameSystem = GameSystem.Get(adventure.GameSystem);
            var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);
            characterSystem.ResetVariables(adventurer);
            return new MessageEditResult<EditResult>(new EditResult(adventurer, gameSystem));
        }
    }

    private record EditResult(Adventurer Adventurer, GameSystem GameSystem);
}
