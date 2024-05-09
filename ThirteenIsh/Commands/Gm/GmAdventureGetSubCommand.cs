using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

internal sealed class GmAdventureGetSubCommand() : SubCommandBase("get", "Gets the details of an adventure.")
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
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var adventureName))
        {
            await command.RespondAsync("Adventure names must contain only letters and spaces.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);
        var adventure = await dataService.GetAdventureAsync(guild, adventureName, cancellationToken);
        if (adventure == null)
        {
            await command.RespondAsync("No such adventure was found in this guild.");
            return;
        }

        var discordService = serviceProvider.GetRequiredService<DiscordService>();
        await discordService.RespondWithAdventureSummaryAsync(dataService, command, adventure, adventure.Name);
    }
}
