using Discord;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Combat;

[MessageHandler(MessageType = typeof(CombatAddMessage))]
internal sealed class CombatAddMessageHandler(SqlDataService dataService, DiscordService discordService, IRandomWrapper random,
    IServiceProvider serviceProvider) : MessageHandlerBase<CombatAddMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        CombatAddMessage message, CancellationToken cancellationToken = default)
    {
        var character = await dataService.GetCharacterAsync(message.Name, message.UserId, CharacterType.Monster, false,
            cancellationToken);
        if (character is null)
        {
            await interaction.ModifyOriginalResponseAsync(properties => properties.Content =
                $"Error getting monster '{message.Name}'. Perhaps they do not exist, or there is more than one character or monster matching that name?");

            return true;
        }

        var result = await dataService.EditEncounterAsync(message.GuildId, message.ChannelId,
            new EditOperation(character, random, message.Rerolls, message.SwarmCount, message.UserId), cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            async output =>
            {
                // Update the encounter table
                var channel = await discordService.GetGuildMessageChannelAsync(message.GuildId, message.ChannelId)
                    ?? throw new InvalidOperationException($"No channel for guild {message.GuildId}, channel {message.ChannelId}");

                var encounterTable = await CommandUtil.UpdateEncounterMessageAsync(serviceProvider, message.GuildId,
                    channel, output.Encounter, output.GameSystem, cancellationToken);

                var user = await discordService.GetGuildUserAsync(message.GuildId, message.UserId);

                // Send an appropriate response
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(user)
                    .WithTitle($"A {character.Name} joined the encounter as {output.Alias} : {output.Result.Roll}")
                    .WithDescription($"{output.Result.Working}\n{encounterTable}");

                await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embedBuilder.Build());
            });

        return true;
    }

    private sealed class EditOperation(Database.Entities.Character character,
        IRandomWrapper random, int rerolls, int swarmCount, ulong userId)
        : SyncEditOperation<EditOutput, EncounterResult>
    {
        public override EditResult<EditOutput> DoEdit(DataContext context, EncounterResult encounterResult)
        {
            var (adventure, encounter) = encounterResult;
            if (adventure.GameSystem != character.GameSystem)
                return CreateError("This monster was not created in the same game system as the adventure.");

            var gameSystem = GameSystem.Get(adventure.GameSystem);
            NameAliasCollection nameAliasCollection = new(encounter);
            var result = gameSystem.EncounterAdd(context, character, encounter, nameAliasCollection,
                random, rerolls, swarmCount, userId, out var alias);

            if (!result.HasValue) return CreateError(
                $"You are not able to add a '{character.Name}' to this encounter at this time.");

            return new EditResult<EditOutput>(new EditOutput(alias, adventure, encounter, gameSystem, result.Value));
        }
    }

    private sealed record EditOutput(string Alias, Adventure Adventure, Encounter Encounter, GameSystem GameSystem,
        GameCounterRollResult Result);
}
