using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

/// <summary>
/// Helper class for checking GM permissions in commands.
/// </summary>
internal static class GmAuthorizationHelper
{
    /// <summary>
    /// Checks if a user has GM permissions for the current guild.
    /// </summary>
    /// <param name="command">The slash command.</param>
    /// <param name="dataService">The data service to fetch guild configuration.</param>
    /// <param name="discordService">The discord service to access guild information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has GM permissions, false otherwise.</returns>
    public static async Task<bool> HasGmPermissionsAsync(SocketSlashCommand command, SqlDataService dataService,
        DiscordService discordService, CancellationToken cancellationToken = default)
    {
        if (command.GuildId is not { } guildId)
        {
            return false;
        }

        try
        {
            var guildUser = await discordService.GetGuildUserAsync(guildId, command.User.Id);
            if (guildUser is not IGuildUser socketGuildUser)
            {
                return false;
            }

            var guild = await dataService.GetGuildAsync(guildId, cancellationToken);

            // Check if a custom GM role is configured
            if (guild.GmRoleId is { } roleId)
            {
                // Check if user has the custom GM role
                if (socketGuildUser.RoleIds.Contains(roleId))
                {
                    return true;
                }
                // If the role was deleted or user doesn't have it, fall back to ManageGuild permission
            }

            // Fall back to checking ManageGuild permission
            return socketGuildUser.GuildPermissions.ManageGuild;
        }
        catch
        {
            // If anything goes wrong (guild not found, etc.), deny access
            return false;
        }
    }

    /// <summary>
    /// Checks GM permissions and responds with an error if the user lacks permissions.
    /// </summary>
    /// <param name="command">The slash command.</param>
    /// <param name="dataService">The data service to fetch guild configuration.</param>
    /// <param name="discordService">The discord service to access guild information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has permissions and the command should continue, false if an error response was sent.</returns>
    public static async Task<bool> CheckGmPermissionsAsync(SocketSlashCommand command, SqlDataService dataService,
        DiscordService discordService, CancellationToken cancellationToken = default)
    {
        if (await HasGmPermissionsAsync(command, dataService, discordService, cancellationToken))
        {
            return true;
        }

        await command.RespondAsync(
            "You do not have permission to use GM commands. You need either the ManageGuild permission or the configured GM role.",
            ephemeral: true);
        return false;
    }
}