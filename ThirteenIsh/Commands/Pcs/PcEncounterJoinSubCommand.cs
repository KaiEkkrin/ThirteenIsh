﻿using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcEncounterJoinSubCommand() : SubCommandBase("join", "Joins the current encounter.")
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
        var embedBuilder = new EmbedBuilder()
            .WithAuthor(command.User)
            .WithTitle($"{output.Adventurer.Name} joined the encounter : {output.Result.Roll}")
            .WithDescription(output.Result.Working);

        await command.RespondAsync(embed: embedBuilder.Build());

        // TODO Update the pinned encounter message.
    }

    private sealed class EditOperation(ulong channelId, ulong userId, IRandomWrapper random, int rerolls)
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

            if (encounter.Combatants.OfType<AdventurerCombatant>().Any(o => o.NativeUserId == userId))
            {
                return new MessageEditResult<EditOutput>(
                    null, "You have already joined this encounter.");
            }

            var gameSystem = GameSystem.Get(guild.CurrentAdventure.GameSystem);
            var result = gameSystem.Logic.EncounterJoin(adventurer, encounter, random, rerolls, userId);
            if (!result.HasValue) return new MessageEditResult<EditOutput>(
                null, "You are not able to join this encounter at this time.");

            return new MessageEditResult<EditOutput>(new EditOutput(adventurer, encounter, result.Value));
        }
    }

    private sealed record EditOutput(Adventurer Adventurer, Encounter Encounter, GameCounterRollResult Result);
}