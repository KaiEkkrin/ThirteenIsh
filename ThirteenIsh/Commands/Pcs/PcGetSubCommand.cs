using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcGetSubCommand(bool asGm)
    : SubCommandBase("get", "Shows your player character in the current adventure.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String,
                asGm ? "The adventurer name." : "Your adventurer name (if you have multiple).",
                isRequired: false)
            .AddOption("full", ApplicationCommandOptionType.Boolean, "Include full character sheet");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        string? name = null;
        CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out name);

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);
        if (await dataService.GetAdventureAsync(guild, null, cancellationToken) is not { } adventure)
        {
            await command.RespondAsync("There is no current adventure.", ephemeral: true);
            return;
        }

        var adventurer = asGm && name != null
            ? await dataService.GetAdventurerAsync(adventure, name, cancellationToken) // GM: any player's adventurer
            : await dataService.GetAdventurerAsync(adventure, command.User.Id, name, cancellationToken); // Player: own adventurer (or default)

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
                Flags = onlyVariables
                    ? CommandUtil.AdventurerSummaryFlags.OnlyVariables
                    : CommandUtil.AdventurerSummaryFlags.WithTags,
                Title = adventurer.Name
            },
            asGm);
    }
}
