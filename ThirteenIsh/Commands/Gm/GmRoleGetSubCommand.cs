using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

internal sealed class GmRoleGetSubCommand() : SubCommandBase("get", "Shows the current GM role for this guild.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId)
        {
            await command.RespondAsync("This command can only be used in a guild.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var discordService = serviceProvider.GetRequiredService<DiscordService>();
        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);

        string responseMessage;
        if (guild.GmRoleId is { } roleId)
        {
            var discordGuild = discordService.GetGuild(guildId);
            var role = discordGuild?.GetRole(roleId);
            if (role is not null)
            {
                responseMessage = $"GM role is currently set to: **{role.Name}**";
            }
            else
            {
                responseMessage = "GM role was set but the role no longer exists. Falling back to ManageGuild permission.";
            }
        }
        else
        {
            responseMessage = "No custom GM role is set. Using ManageGuild permission.";
        }

        await command.RespondAsync(responseMessage, ephemeral: true);
    }
}