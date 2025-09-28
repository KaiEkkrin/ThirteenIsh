using Discord;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Pcs;

[MessageHandler(MessageType = typeof(PcUpdateMessage))]
internal sealed class PcUpdateMessageHandler(SqlDataService dataService) : MessageHandlerBase<PcUpdateMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        PcUpdateMessage message, CancellationToken cancellationToken = default)
    {
        var result = await dataService.EditAdventurerAsync(
            message.GuildId, message.UserId, new EditOperation(dataService, message.UserId), cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            adventurer =>
            {
                var gameSystem = GameSystem.Get(adventurer.Adventure.GameSystem);
                var embed = CommandUtil.BuildTrackedCharacterSummaryEmbed(interaction, adventurer, gameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        Flags = CommandUtil.AdventurerSummaryFlags.WithTags,
                        Title = $"Updated {adventurer.Name}"
                    });

                return interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }

    private sealed class EditOperation(SqlDataService dataService, ulong userId)
        : EditOperation<Adventurer, Adventurer>
    {
        public override async Task<EditResult<Adventurer>> DoEditAsync(DataContext context, Adventurer adventurer,
            CancellationToken cancellationToken)
        {
            var character = await dataService.GetCharacterAsync(adventurer.Name, userId, CharacterType.PlayerCharacter,
                false, cancellationToken);

            if (character is null)
                return CreateError($"Character {adventurer.Name} not found.");

            adventurer.LastUpdated = DateTimeOffset.UtcNow;
            adventurer.Sheet = character.Sheet;

            // Scrub the adventurer's fixes and variables -- don't leave lying around any for custom
            // counters that no longer exist
            var gameSystem = GameSystem.Get(character.GameSystem);
            var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter, character.CharacterSystemName);
            characterSystem.ScrubCustomCounters(adventurer);

            return new EditResult<Adventurer>(adventurer);
        }
    }
}
