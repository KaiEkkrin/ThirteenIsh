using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Adventures;

internal sealed class AdventureAddSubCommand() : SubCommandBase("add", "Adds a new adventure.")
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

        if (!CommandUtil.TryGetOption<string>(option, "description", out var description))
            description = string.Empty;

        // This will also make it the current adventure
        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.CreateAdventureAsync(name, description, guildId, cancellationToken);
        if (guild?.CurrentAdventure is null)
        {
            await command.RespondAsync($"Cannot create an adventure named '{name}'. Perhaps it was already created?",
                ephemeral: true);
            return;
        }

        await CommandUtil.RespondWithAdventureSummaryAsync(command, guild, guild.CurrentAdventure, $"Created adventure: {name}");
    }
}
