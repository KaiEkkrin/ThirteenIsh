using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

internal sealed class CombatJoinSubCommand() : SubCommandBase("join", "Joins the current encounter.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddRerollsOption("rerolls");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        if (!CommandUtil.TryGetOption<int>(option, "rerolls", out var rerolls)) rerolls = 0;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var result = await dataService.EditEncounterAsync(guildId, channelId,
            new EditOperation(dataService, command.User.Id, random, rerolls), cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            async output =>
            {
                await command.DeferAsync();

                // Update the encounter table
                var encounterTable = await CommandUtil.UpdateEncounterMessageAsync(serviceProvider, guildId,
                    command.Channel, output.Encounter, output.GameSystem, cancellationToken);

                // Send an appropriate response
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(command.User)
                    .WithTitle($"{output.Adventurer.Name} joined the encounter : {output.Result.Roll}")
                    .WithDescription($"{output.Result.Working}\n{encounterTable}");

                await command.ModifyOriginalResponseAsync(properties => properties.Embed = embedBuilder.Build());
            });
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

            if (!result.HasValue) return CreateError("You are not able to join this encounter at this time.");

            return new EditResult<EditOutput>(new EditOutput(
                adventure, adventurer, encounter, gameSystem, result.Value));
        }
    }

    private sealed record EditOutput(Adventure Adventure, Adventurer Adventurer, Encounter Encounter, GameSystem GameSystem,
        GameCounterRollResult Result);
}
