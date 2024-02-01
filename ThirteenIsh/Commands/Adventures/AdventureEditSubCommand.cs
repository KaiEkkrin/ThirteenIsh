using Discord;
using Discord.WebSocket;
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
        var guild = await dataService.EditAdventureAsync(name, description, guildId, cancellationToken);
        var adventure = guild?.Adventures.FirstOrDefault(o => o.Name == name);
        if (guild is null || adventure is null)
        {
            await command.RespondAsync($"Cannot edit an adventure named '{name}'. Perhaps it does not exist?",
                ephemeral: true);
            return;
        }

        await CommandUtil.RespondWithAdventureSummaryAsync(command, guild, adventure, adventure.Name);
    }
}
