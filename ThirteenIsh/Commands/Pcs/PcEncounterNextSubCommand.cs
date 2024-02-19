using Discord;
using Discord.WebSocket;
using Microsoft.VisualBasic;
using System.Globalization;
using System.Text;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

/// <summary>
/// Moves to the next combatant in the encounter, i.e. the end-of-turn command.
/// I'm making this a command anyone can use because my players will be used to it from Avrae.
/// TODO Consider making the end-of-round command (as opposed to the end-of-turn command)
/// game master only (make it `adventure encounter next-turn` or something) because that one
/// is not going to be reversible and so trolling players could troll with the turn roll-over.
/// See this basic thing working first, though.
/// Also make a `prev` command to go to the previous combatant and a `swap` command to swap two
/// combatants in the initiative.
/// </summary>
internal sealed class PcEncounterNextSubCommand() : SubCommandBase("next", "Moves on to the next combatant in the encounter.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var (output, message) = await dataService.EditGuildAsync(
            new EditOperation(channelId, command.User.Id, random), guildId, cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await command.RespondAsync(message, ephemeral: true);
            return;
        }

        if (output is null) throw new InvalidOperationException(nameof(output));

        // Update the encounter table
        var encounterTable = output.GameSystem.Logic.EncounterTable(output.Encounter);
        var pinnedMessageService = serviceProvider.GetRequiredService<PinnedMessageService>();
        await pinnedMessageService.SetEncounterMessageAsync(command.Channel, output.Encounter.AdventureName, guildId,
            encounterTable, cancellationToken);

        // Send an appropriate response
        StringBuilder titleBuilder = new();
        if (!string.IsNullOrEmpty(output.PreviousCombatantName))
        {
            titleBuilder.Append(CultureInfo.CurrentCulture, $"{output.PreviousCombatantName} finished their turn. ");
        }

        titleBuilder.Append(CultureInfo.CurrentCulture, $"It is now {output.CurrentCombatantName}'s turn.");

        var embedBuilder = new EmbedBuilder()
            .WithAuthor(command.User)
            .WithTitle(titleBuilder.ToString());

        await command.RespondAsync(embed: embedBuilder.Build());
    }

    private sealed class EditOperation(ulong channelId, ulong userId, IRandomWrapper random)
        : SyncEditOperation<ResultOrMessage<EditOutput>, Guild, MessageEditResult<EditOutput>>
    {
        public override MessageEditResult<EditOutput> DoEdit(Guild guild)
        {
            if (guild.CurrentAdventure?.Adventurers.TryGetValue(userId, out var adventurer) != true ||
                adventurer is null)
            {
                return new MessageEditResult<EditOutput>(
                    null, "Either there is no current adventure or you have not joined it.");
            }

            if (!guild.Encounters.TryGetValue(channelId, out var encounter))
            {
                return new MessageEditResult<EditOutput>(
                    null, "No encounter is currently in progress in this channel.");
            }

            var previousCombatantName = encounter.TurnIndex.HasValue
                ? encounter.Combatants[encounter.TurnIndex.Value].Name
                : null;

            var gameSystem = GameSystem.Get(guild.CurrentAdventure.GameSystem);
            if (gameSystem.Logic.EncounterNext(encounter, random) is not { } turnIndex)
                return new MessageEditResult<EditOutput>(null, "This encounter cannot be progressed at this time.");

            return new MessageEditResult<EditOutput>(new EditOutput(
                previousCombatantName, encounter.Combatants[turnIndex].Name, encounter, gameSystem));
        }
    }

    private sealed record EditOutput(string? PreviousCombatantName, string CurrentCombatantName, Encounter Encounter,
        GameSystem GameSystem);
}
