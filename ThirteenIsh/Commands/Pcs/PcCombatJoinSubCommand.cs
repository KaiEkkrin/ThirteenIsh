using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcCombatJoinSubCommand() : SubCommandBase("join", "Joins the current encounter.")
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
        var (output, message) = await dataService.EditEncounterAsync(guildId, channelId,
            new EditOperation(dataService, command.User.Id, random, rerolls), cancellationToken);

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
        var embedBuilder = new EmbedBuilder()
            .WithAuthor(command.User)
            .WithTitle($"{output.Adventurer.Name} joined the encounter : {output.Result.Roll}")
            .WithDescription($"{output.Result.Working}\n{encounterTable}");

        await command.RespondAsync(embed: embedBuilder.Build());
    }

    private sealed class EditOperation(SqlDataService dataService, ulong userId, IRandomWrapper random, int rerolls)
        : EditOperation<ResultOrMessage<EditOutput>, EncounterResult, MessageEditResult<EditOutput>>
    {
        public override async Task<MessageEditResult<EditOutput>> DoEditAsync(DataContext context,
            EncounterResult encounterResult, CancellationToken cancellationToken = default)
        {
            var (adventure, encounter) = encounterResult;
            if (encounter.Combatants.OfType<AdventurerCombatant>().Any(o => o.UserId == userId))
            {
                return new MessageEditResult<EditOutput>(
                    null, "You have already joined this encounter.");
            }

            var adventurer = await dataService.GetAdventurerAsync(adventure, userId, cancellationToken);
            if (adventurer is null) return new MessageEditResult<EditOutput>(
                null, "You have not joined this adventure.");

            var gameSystem = GameSystem.Get(adventure.GameSystem);
            NameAliasCollection nameAliasCollection = new(encounter);

            var result = gameSystem.EncounterJoin(context, adventurer, encounter, nameAliasCollection,
                random, rerolls, userId);

            if (!result.HasValue) return new MessageEditResult<EditOutput>(
                null, "You are not able to join this encounter at this time.");

            return new MessageEditResult<EditOutput>(new EditOutput(
                adventure, adventurer, encounter, gameSystem, result.Value));
        }
    }

    private sealed record EditOutput(Adventure Adventure, Adventurer Adventurer, Encounter Encounter, GameSystem GameSystem,
        GameCounterRollResult Result);
}
