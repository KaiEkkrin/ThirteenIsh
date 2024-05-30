﻿using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcGetSubCommand(bool asGm)
    : SubCommandBase("get", "Shows your player character in the current adventure.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOptionIf(asGm, builder => builder.AddOption("name", ApplicationCommandOptionType.String,
                "The character name.", isRequired: true))
            .AddOption("full", ApplicationCommandOptionType.Boolean, "Include full character sheet");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        string? name = null;
        if (asGm && !CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out name))
        {
            await command.RespondAsync(
                $"{CharacterType.PlayerCharacter.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter)} names must contain only letters and spaces",
                ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);
        if (await dataService.GetAdventureAsync(guild, null, cancellationToken) is not { } adventure)
        {
            await command.RespondAsync("There is no current adventure.", ephemeral: true);
            return;
        }

        var adventurer = name != null // only as GM
            ? await dataService.GetAdventurerAsync(adventure, name, cancellationToken)
            : await dataService.GetAdventurerAsync(adventure, command.User.Id, cancellationToken);

        if (adventurer == null)
        {
            await command.RespondAsync("No player character found in this adventure.", ephemeral: true);
            return;
        }

        var gameSystem = GameSystem.Get(adventure.GameSystem);
        var onlyVariables = !CommandUtil.TryGetOption<bool>(option, "full", out var full) || !full;
        await CommandUtil.RespondWithTrackedCharacterSummaryAsync(command, adventurer, gameSystem,
            new CommandUtil.AdventurerSummaryOptions
            {
                OnlyVariables = onlyVariables,
                Title = adventurer.Name
            });
    }
}
