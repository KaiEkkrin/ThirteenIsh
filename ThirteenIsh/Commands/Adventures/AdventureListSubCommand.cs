﻿using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Adventures;

internal sealed class AdventureListSubCommand() : SubCommandBase("list", "Lists the adventures in this guild.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithTitle("Adventures");

        foreach (var adventure in guild.Adventures.OrderBy(o => o.Name))
        {
            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithIsInline(false)
                .WithName(adventure.Name == guild.CurrentAdventureName ? $"{adventure.Name} [Current]" : adventure.Name)
                .WithValue($"{adventure.Adventurers.Count} adventurers"));
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}