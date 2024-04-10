using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcLeaveSubCommand() : SubCommandBase("leave", "Leaves the current adventure.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        var adventure = await dataService.GetAdventureAsync(guild, null, cancellationToken);
        if (adventure is null)
        {
            await command.RespondAsync($"There is no current adventure in this guild.");
            return;
        }

        // Supply a confirm button
        LeaveAdventureMessage message = new()
        {
            GuildId = guildId,
            Name = adventure.Name,
            UserId = command.User.Id
        };

        await dataService.AddMessageAsync(message, cancellationToken);

        ComponentBuilder builder = new();
        builder.WithButton("Leave", message.GetMessageId(), ButtonStyle.Danger);

        await command.RespondAsync(
            $"Do you really want to leave the adventure named '{adventure.Name}'? This cannot be undone.",
            ephemeral: true, components: builder.Build());
    }
}
