using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcGetSubCommand() : SubCommandBase("get", "Shows your player character in the current adventure.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        if (guild.CurrentAdventure?.Adventurers.TryGetValue(command.User.Id, out var adventurer) != true ||
            adventurer is null)
        {
            await command.RespondAsync($"Either there is no current adventure or you have not joined it.",
                ephemeral: true);
            return;
        }

        await CommandUtil.RespondWithAdventurerSummaryAsync(command, adventurer, adventurer.Name);
    }
}
