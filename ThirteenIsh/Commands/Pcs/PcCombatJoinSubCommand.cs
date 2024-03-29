using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
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

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var (output, message) = await dataService.EditGuildAsync(
            new EditOperation(channelId, command.User.Id, random, rerolls), guildId, cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await command.RespondAsync(message, ephemeral: true);
            return;
        }

        if (output is null) throw new InvalidOperationException(nameof(output));

        // Update the encounter table
        var encounterTable = output.GameSystem.EncounterTable(output.Adventure, output.Encounter);
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

    private sealed class EditOperation(ulong channelId, ulong userId, IRandomWrapper random, int rerolls)
        : SyncEditOperation<ResultOrMessage<EditOutput>, Guild, MessageEditResult<EditOutput>>
    {
        public override MessageEditResult<EditOutput> DoEdit(Guild guild)
        {
            if (!CommandUtil.TryGetCurrentCombatant(guild, channelId, userId, out var adventure, out var adventurer,
                out var encounter, out var errorMessage))
            {
                return new MessageEditResult<EditOutput>(null, errorMessage);
            }

            if (encounter.Combatants.OfType<AdventurerCombatant>().Any(o => o.NativeUserId == userId))
            {
                return new MessageEditResult<EditOutput>(
                    null, "You have already joined this encounter.");
            }

            var gameSystem = GameSystem.Get(adventure.GameSystem);
            var nameAliasCollection = encounter.BuildNameAliasCollection();
            var result = gameSystem.EncounterJoin(adventurer, encounter, nameAliasCollection, random, rerolls, userId);
            if (!result.HasValue) return new MessageEditResult<EditOutput>(
                null, "You are not able to join this encounter at this time.");

            return new MessageEditResult<EditOutput>(new EditOutput(
                adventure, adventurer, encounter, gameSystem, result.Value));
        }
    }

    private sealed record EditOutput(Adventure Adventure, Adventurer Adventurer, Encounter Encounter, GameSystem GameSystem,
        GameCounterRollResult Result);
}
