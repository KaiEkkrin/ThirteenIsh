using Discord;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Combat;

[MessageHandler(MessageType = typeof(CombatJoinMessage))]
internal sealed class CombatJoinMessageHandler(SqlDataService dataService, DiscordService discordService, IRandomWrapper random,
    IServiceProvider serviceProvider) : MessageHandlerBase<CombatJoinMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        CombatJoinMessage message, CancellationToken cancellationToken = default)
    {
        var result = await dataService.EditEncounterAsync(message.GuildId, message.ChannelId,
            new EditOperation(dataService, message.UserId, random, message.Rerolls), cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            async output =>
            {
                var channel = await discordService.GetGuildMessageChannelAsync(message.GuildId, message.ChannelId)
                    ?? throw new InvalidOperationException($"No channel for guild {message.GuildId}, channel {message.ChannelId}");

                // Update the encounter table
                var encounterTable = await CommandUtil.UpdateEncounterMessageAsync(serviceProvider, message.GuildId,
                    channel, output.Encounter, output.GameSystem, cancellationToken);

                var user = await discordService.GetGuildUserAsync(message.GuildId, message.UserId);

                // Send an appropriate response
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(user)
                    .WithTitle($"{output.Adventurer.Name} joined the encounter : {output.Result.Roll}")
                    .WithDescription($"{output.Result.Working}\n{encounterTable}");

                await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embedBuilder.Build());
            });

        return true;
    }

    private sealed class EditOperation(SqlDataService dataService, ulong userId, IRandomWrapper random, int rerolls)
        : EditOperation<EditOutput, EncounterResult>
    {
        public override async Task<EditResult<EditOutput>> DoEditAsync(DataContext context,
            EncounterResult encounterResult, CancellationToken cancellationToken = default)
        {
            var (adventure, encounter) = encounterResult;
            if (encounter.Combatants.OfType<AdventurerCombatant>().Any(o => o.UserId == userId))
            {
                return CreateError("You have already joined this encounter.");
            }

            var adventurer = await dataService.GetAdventurerAsync(adventure, userId, cancellationToken);
            if (adventurer is null) return CreateError("You have not joined this adventure.");

            var gameSystem = GameSystem.Get(adventure.GameSystem);
            NameAliasCollection nameAliasCollection = new(encounter);

            var result = gameSystem.EncounterJoin(context, adventurer, encounter, nameAliasCollection,
                random, rerolls, userId);

            if (result.Error != GameCounterRollError.Success)
                return CreateError($"Unable to join this encounter : {result.ErrorMessage}.");

            return new EditResult<EditOutput>(new EditOutput(adventure, adventurer, encounter, gameSystem, result));
        }
    }

    private sealed record EditOutput(Adventure Adventure, Adventurer Adventurer, Encounter Encounter, GameSystem GameSystem,
        EncounterRollResult Result);
}
