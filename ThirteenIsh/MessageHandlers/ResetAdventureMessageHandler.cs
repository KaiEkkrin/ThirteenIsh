using Discord;
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
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        ResetAdventurerMessage message, CancellationToken cancellationToken = default)
    {
        var result = await dataService.EditAdventurerAsync(
            message.GuildId, message.UserId, new EditOperation(), message.AdventurerName, cancellationToken);

        await result.Handle(
            errorMessage => interaction.RespondAsync(errorMessage, ephemeral: true),
            output =>
            {
                return CommandUtil.RespondWithTrackedCharacterSummaryAsync(interaction, output.Adventurer, output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        Flags = CommandUtil.AdventurerSummaryFlags.OnlyVariables,
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
            var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter, adventurer.CharacterSystemName);
            characterSystem.ResetVariables(adventurer);
            return new EditResult<EditResult>(new EditResult(adventurer, gameSystem));
        }
    }

    private record EditResult(Adventurer Adventurer, GameSystem GameSystem);
}
