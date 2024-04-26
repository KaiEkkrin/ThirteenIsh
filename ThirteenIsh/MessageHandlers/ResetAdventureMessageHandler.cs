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
        var result = await dataService.EditAdventurerAsync(
            message.GuildId, message.UserId, new EditOperation(), cancellationToken);

        await result.Handle(
            errorMessage => component.RespondAsync(errorMessage, ephemeral: true),
            output =>
            {
                return CommandUtil.RespondWithAdventurerSummaryAsync(component, output.Adventurer, output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        OnlyVariables = true,
                        Title = $"Reset adventurer {output.Adventurer.Name}"
                    });
            });

        return true;
    }

    private sealed class EditOperation()
        : SyncEditOperation<EditResult, Adventurer>
    {
        public override EditResult<EditResult> DoEdit(DataContext context, Adventurer adventurer)
        {
            var gameSystem = GameSystem.Get(adventurer.Adventure.GameSystem);
            var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);
            characterSystem.ResetVariables(adventurer);
            return new EditResult<EditResult>(new EditResult(adventurer, gameSystem));
        }
    }

    private record EditResult(Adventurer Adventurer, GameSystem GameSystem);
}
