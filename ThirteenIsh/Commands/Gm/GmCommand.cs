using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

// short for "Game Master" -- game master only commands.
internal sealed class GmCommand() : CommandBase("gm", "Game Master commands.",
    new GmAdventureSubCommandGroup(),
    new GmCombatSubCommandGroup(),
    new GmPcSubCommandGroup(),
    new GmRoleSubCommandGroup())
{
    protected override async Task<bool> CheckPermissionsAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var discordService = serviceProvider.GetRequiredService<DiscordService>();

        return await GmAuthorizationHelper.CheckGmPermissionsAsync(command, dataService, discordService, cancellationToken);
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        // GM permissions are now handled by overriding CheckPermissionsAsync
        return base.CreateBuilder();
    }
}
