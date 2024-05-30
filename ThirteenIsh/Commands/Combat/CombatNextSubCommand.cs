using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

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
internal sealed class CombatNextSubCommand() : SubCommandBase("next", "Moves on to the next combatant in the encounter.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var result = await dataService.EditEncounterAsync(
            guildId, channelId, new EditOperation(random), cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            async output =>
            {
                // Update the encounter table
                var encounterTable = await CommandUtil.UpdateEncounterMessageAsync(serviceProvider, guildId,
                    command.Channel, output.Encounter, output.GameSystem, cancellationToken);

                // Send an appropriate response
                StringBuilder titleBuilder = new();
                if (!string.IsNullOrEmpty(output.PreviousCombatantAlias))
                {
                    titleBuilder.Append(CultureInfo.CurrentCulture,
                        $"{output.PreviousCombatantAlias} finished their turn. ");
                }

                titleBuilder.Append(CultureInfo.CurrentCulture, $"It is now {output.CurrentCombatantAlias}'s turn.");

                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(command.User)
                    .WithTitle(titleBuilder.ToString())
                    .WithDescription(encounterTable);

                await command.RespondAsync(embed: embedBuilder.Build());
            });
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
