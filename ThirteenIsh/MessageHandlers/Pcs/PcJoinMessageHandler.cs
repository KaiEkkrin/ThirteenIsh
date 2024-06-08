using Discord;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Pcs;

[MessageHandler(MessageType = typeof(PcJoinMessage))]
internal sealed class PcJoinMessageHandler(SqlDataService dataService, DiscordService discordService)
    : MessageHandlerBase<PcJoinMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        PcJoinMessage message, CancellationToken cancellationToken = default)
    {
        var character = await dataService.GetCharacterAsync(message.CharacterName, message.UserId, CharacterType.PlayerCharacter,
            false, cancellationToken);

        if (character is null)
        {
            await interaction.ModifyOriginalResponseAsync(properties => properties.Content = "Character not found");
            return true;
        }

        var result = await dataService.EditAdventureAsync(
            message.GuildId, new EditOperation(dataService, character), cancellationToken: cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            async adventure =>
            {
                var user = await discordService.GetGuildUserAsync(message.GuildId, message.UserId);

                EmbedBuilder embedBuilder = new();
                embedBuilder.WithAuthor(user);
                embedBuilder.WithTitle($"Joined {adventure.Name} as {character.Name}");

                return await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embedBuilder.Build());
            });

        return true;
    }

    private sealed class EditOperation(SqlDataService dataService, Database.Entities.Character character)
        : EditOperation<Adventure, Adventure>
    {
        public override async Task<EditResult<Adventure>> DoEditAsync(DataContext context, Adventure adventure,
            CancellationToken cancellationToken = default)
        {
            if (adventure.GameSystem != character.GameSystem)
                return CreateError("This character was not created in the same game system as the adventure.");

            var currentAdventurer = await dataService.GetAdventurerAsync(adventure, character.UserId,
                cancellationToken);
            if (currentAdventurer == null)
            {
                Adventurer adventurer = new()
                {
                    Name = character.Name,
                    LastUpdated = DateTimeOffset.UtcNow,
                    Sheet = character.Sheet,
                    UserId = character.UserId
                };

                var gameSystem = GameSystem.Get(character.GameSystem);
                var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);
                characterSystem.ResetVariables(adventurer);
                adventure.Adventurers.Add(adventurer);
                return new EditResult<Adventure>(adventure);
            }
            else if (currentAdventurer.Name == character.Name)
            {
                return CreateError("This character is already joined to the current adventure.");
            }
            else
            {
                return CreateError("You have already joined this adventure with a different character.");
            }
        }
    }
}
