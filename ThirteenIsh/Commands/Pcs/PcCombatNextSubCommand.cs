using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
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
internal sealed class PcCombatNextSubCommand() : SubCommandBase("next", "Moves on to the next combatant in the encounter.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var (output, message) = await dataService.EditEncounterAsync(
            guildId, channelId, new EditOperation(random), cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await command.RespondAsync(message, ephemeral: true);
            return;
        }

        if (output is null) throw new InvalidOperationException(nameof(output));

        // Update the encounter table
        var encounterTable = await output.GameSystem.BuildEncounterTableAsync(dataService, output.Adventure,
            output.Encounter, cancellationToken);

        var pinnedMessageService = serviceProvider.GetRequiredService<PinnedMessageService>();
        await pinnedMessageService.SetEncounterMessageAsync(command.Channel, output.Encounter.AdventureName, guildId,
            encounterTable, cancellationToken);

        // Send an appropriate response
        StringBuilder titleBuilder = new();
        if (!string.IsNullOrEmpty(output.PreviousCombatantAlias))
        {
            titleBuilder.Append(CultureInfo.CurrentCulture, $"{output.PreviousCombatantAlias} finished their turn. ");
        }

        titleBuilder.Append(CultureInfo.CurrentCulture, $"It is now {output.CurrentCombatantAlias}'s turn.");

        var embedBuilder = new EmbedBuilder()
            .WithAuthor(command.User)
            .WithTitle(titleBuilder.ToString())
            .WithDescription(encounterTable);

        await command.RespondAsync(embed: embedBuilder.Build());
    }

    private sealed class EditOperation(IRandomWrapper random)
        : SyncEditOperation<ResultOrMessage<EditOutput>, EncounterResult, MessageEditResult<EditOutput>>
    {
        public override MessageEditResult<EditOutput> DoEdit(EncounterResult result)
        {
            var (adventure, encounter) = result;
            var previousCombatantAlias = encounter.TurnAlias;

            var gameSystem = GameSystem.Get(adventure.GameSystem);
            if (gameSystem.EncounterNext(encounter, random) is not { } nextCombatant)
                return new MessageEditResult<EditOutput>(null, "This encounter cannot be progressed at this time.");

            return new MessageEditResult<EditOutput>(new EditOutput(
                previousCombatantAlias, nextCombatant.Alias, adventure, encounter, gameSystem));
        }
    }

    private sealed record EditOutput(string? PreviousCombatantAlias, string CurrentCombatantAlias,
        Adventure Adventure, Encounter Encounter, GameSystem GameSystem);
}
