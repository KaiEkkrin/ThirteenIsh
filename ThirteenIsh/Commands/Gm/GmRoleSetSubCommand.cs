using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

internal sealed class GmRoleSetSubCommand() : SubCommandBase("set", "Sets the GM role for this guild.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("role", ApplicationCommandOptionType.String, "The role name to use for GM permissions. Use 'clear' to remove custom role.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId)
        {
            await command.RespondAsync("This command can only be used in a guild.", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "role", out var roleName))
        {
            await command.RespondAsync("Role name is required.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var discordService = serviceProvider.GetRequiredService<DiscordService>();
        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);

        // Handle clearing the role
        if (string.Equals(roleName, "clear", StringComparison.OrdinalIgnoreCase))
        {
            guild.GmRoleId = null;
            await dataService.SaveChangesAsync(cancellationToken);
            await command.RespondAsync("GM role cleared. Now using ManageGuild permission.", ephemeral: true);
            return;
        }

        // Get the Discord guild to access roles
        var discordGuild = discordService.GetGuild(guildId);
        if (discordGuild is null)
        {
            await command.RespondAsync("Guild not found.", ephemeral: true);
            return;
        }

        // Find the role by name
        var role = discordGuild.Roles.Where(r => !r.IsEveryone).FirstOrDefault(r =>
            string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));

        if (role is null)
        {
            var availableRoles = string.Join(", ", discordGuild.Roles
                .Where(r => !r.IsEveryone)
                .OrderBy(r => r.Name)
                .Take(10)
                .Select(r => r.Name));

            await command.RespondAsync(
                $"Role '{roleName}' not found. Available roles: {availableRoles}",
                ephemeral: true);
            return;
        }

        // Set the role
        guild.GmRoleId = role.Id;
        await dataService.SaveChangesAsync(cancellationToken);

        await command.RespondAsync(
            $"GM role set to: **{role.Name}**. Users with this role can now use GM commands.",
            ephemeral: true);
    }
}