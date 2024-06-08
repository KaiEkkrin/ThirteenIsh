using Discord;
using ThirteenIsh.ChannelMessages.Gm;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Gm;

[MessageHandler(MessageType = typeof(GmCombatRemoveMessage))]
internal sealed class GmCombatRemoveMessageHandler(SqlDataService dataService, DiscordService discordService,
    IServiceProvider serviceProvider) : MessageHandlerBase<GmCombatRemoveMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        GmCombatRemoveMessage message, CancellationToken cancellationToken = default)
    {
        var result = await dataService.EditEncounterAsync(
            message.GuildId, message.ChannelId, new EditOperation(message.Alias), cancellationToken);

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

                // Send a response
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(user)
                    .WithTitle($"'{message.Alias}' was removed from the combat.")
                    .WithDescription(encounterTable);

                await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embedBuilder.Build());
            });

        return true;
    }

    private sealed class EditOperation(string alias)
        : SyncEditOperation<EditOutput, EncounterResult>
    {
        public override EditResult<EditOutput> DoEdit(DataContext context, EncounterResult param)
        {
            var (adventure, encounter) = param;
            return encounter.RemoveCombatant(alias) switch
            {
                CombatantRemoveResult.Success => new EditResult<EditOutput>(new EditOutput(adventure, encounter,
                    GameSystem.Get(adventure.GameSystem))),

                CombatantRemoveResult.NotFound => CreateError($"There is no combatant matching alias '{alias}'."),
                CombatantRemoveResult.IsTheirTurn =>
                    CreateError($"'{alias}' cannot be removed, because it is currently their turn."),

                { } unrecognisedResult => throw new InvalidOperationException(
                    $"Unrecognised remove combatant result: {unrecognisedResult}")
            };
        }
    }

    private sealed record EditOutput(Adventure Adventure, Encounter Encounter, GameSystem GameSystem);
}
