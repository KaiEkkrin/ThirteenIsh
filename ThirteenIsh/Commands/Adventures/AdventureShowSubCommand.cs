using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Adventures;

internal sealed class AdventureShowSubCommand() : SubCommandBase("show", "Shows the details of an adventure.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The adventure name.");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);

        if (!CommandUtil.TryGetSelectedAdventure(guild, option, "name", out var adventure))
        {
            await command.RespondAsync("No such adventure was found in this guild.");
            return;
        }

        await CommandUtil.RespondWithAdventureSummaryAsync(command, guild, adventure, adventure.Name);
    }
}
