﻿using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Adventures;

internal sealed class AdventureEditSubCommand() : SubCommandBase("edit", "Edits an adventure.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The adventure name.",
                isRequired: true)
            .AddOption("description", ApplicationCommandOptionType.String, "A description of the adventure.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync("Adventure names must contain only letters and spaces", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "description", out var description)) description = string.Empty;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var updatedGuild = await dataService.EditGuildAsync(
            new EditOperation(name, description), guildId, cancellationToken);

        if (updatedGuild?.Adventures.FirstOrDefault(o => o.Name == name) is not { } adventure)
        {
            await command.RespondAsync($"Cannot edit an adventure named '{name}'. Perhaps it does not exist?",
                ephemeral: true);
            return;
        }

        var discordService = serviceProvider.GetRequiredService<DiscordService>();
        await discordService.RespondWithAdventureSummaryAsync(command, updatedGuild, adventure, adventure.Name);
    }

    private sealed class EditOperation(string name, string description) : SyncEditOperation<Guild, Guild, EditResult<Guild>>
    {
        public override EditResult<Guild> DoEdit(Guild guild)
        {
            var index = guild.Adventures.FindIndex(o => o.Name == name);
            if (index < 0) return new EditResult<Guild>(null);

            guild.Adventures[index].Description = description;
            return new EditResult<Guild>(guild);
        }
    }
}