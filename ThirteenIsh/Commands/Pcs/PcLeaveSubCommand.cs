using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcLeaveSubCommand() : SubCommandBase("leave", "Leaves the current adventure.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        if (guild.CurrentAdventure is null)
        {
            await command.RespondAsync($"There is no current adventure in this guild.4?");
            return;
        }

        // Supply a confirm button
        LeaveAdventureMessage message = new()
        {
            GuildId = (long)guildId,
            Name = guild.CurrentAdventure.Name,
            UserId = (long)command.User.Id
        };

        await dataService.AddMessageAsync(message, cancellationToken);

        ComponentBuilder builder = new();
        builder.WithButton("Leave", message.GetMessageId(), ButtonStyle.Danger);

        await command.RespondAsync(
            $"Do you really want to leave the adventure named '{guild.CurrentAdventure.Name}'? This cannot be undone.",
            ephemeral: true, components: builder.Build());
    }
}
