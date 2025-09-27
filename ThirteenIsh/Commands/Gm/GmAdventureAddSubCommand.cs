using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

internal sealed class GmAdventureAddSubCommand() : SubCommandBase("add", "Adds a new adventure.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The adventure name.",
                isRequired: true)
            .AddOption("description", ApplicationCommandOptionType.String, "A description of the adventure.",
                isRequired: true)
            .AddOption(GameSystem.BuildGameSystemChoiceOption("game-system"));
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

        if (!CommandUtil.TryGetOption<string>(option, "description", out var description))
            description = string.Empty;

        if (!CommandUtil.TryGetOption<string>(option, "game-system", out var gameSystemName) ||
            GameSystem.AllGameSystems.FirstOrDefault(o => o.Name == gameSystemName) is not { } gameSystem)
        {
            await command.RespondAsync("Must choose a recognised game system", ephemeral: true);
            return;
        }

        // This will also make it the current adventure
        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var adventure = await dataService.AddAdventureAsync(guildId, name, description, gameSystem.Name,
            cancellationToken);

        if (adventure is null)
        {
            await command.RespondAsync(
                $"Error creating adventure '{name}'. Perhaps an adventure with that name already exists.",
                ephemeral: true);
            return;
        }

        var discordService = serviceProvider.GetRequiredService<DiscordService>();
        await discordService.RespondWithAdventureSummaryAsync(dataService, command, adventure.Adventure,
            $"Created adventure: {name}");
    }
}
