using Discord;
using System.Globalization;
using System.Text;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Combat;

[MessageHandler(MessageType = typeof(CombatNextMessage))]
internal sealed class CombatNextMessageHandler(SqlDataService dataService, DiscordService discordService, IRandomWrapper random,
    IServiceProvider serviceProvider) : MessageHandlerBase<CombatNextMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        CombatNextMessage message, CancellationToken cancellationToken = default)
    {
        var result = await dataService.EditEncounterAsync(
            message.GuildId, message.ChannelId, new EditOperation(random), cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            async output =>
            {
                var channel = await discordService.GetGuildMessageChannelAsync(message.GuildId, message.ChannelId)
                    ?? throw new InvalidOperationException($"No channel for guild {message.GuildId}, channel {message.ChannelId}");

                // Update the encounter table
                var encounterTable = await CommandUtil.UpdateEncounterMessageAsync(serviceProvider, message.GuildId,
                    channel, output.Encounter, output.GameSystem, cancellationToken);

                // Send an appropriate response
                StringBuilder titleBuilder = new();
                if (!string.IsNullOrEmpty(output.PreviousCombatantAlias))
                {
                    titleBuilder.Append(CultureInfo.CurrentCulture,
                        $"{output.PreviousCombatantAlias} finished their turn. ");
                }

                titleBuilder.Append(CultureInfo.CurrentCulture, $"It is now {output.CurrentCombatantAlias}'s turn.");

                var user = await discordService.GetGuildUserAsync(message.GuildId, message.UserId);
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(user)
                    .WithTitle(titleBuilder.ToString())
                    .WithDescription(encounterTable);

                await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embedBuilder.Build());
            });

        return true;
    }

    private sealed class EditOperation(IRandomWrapper random)
        : SyncEditOperation<EditOutput, EncounterResult>
    {
        public override EditResult<EditOutput> DoEdit(DataContext context, EncounterResult result)
        {
            var (adventure, encounter) = result;
            var previousCombatantAlias = encounter.TurnAlias;

            var gameSystem = GameSystem.Get(adventure.GameSystem);
            if (gameSystem.EncounterNext(encounter, random) is not { } nextCombatant)
                return CreateError("This encounter cannot be progressed at this time.");

            return new EditResult<EditOutput>(new EditOutput(
                previousCombatantAlias, nextCombatant.Alias, adventure, encounter, gameSystem));
        }
    }

    private sealed record EditOutput(string? PreviousCombatantAlias, string CurrentCombatantAlias,
        Adventure Adventure, Encounter Encounter, GameSystem GameSystem);
}
