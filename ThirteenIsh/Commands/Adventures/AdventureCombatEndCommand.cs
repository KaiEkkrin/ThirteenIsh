using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Adventures;

internal sealed class AdventureCombatEndCommand() : SubCommandBase("end", "Ends an encounter in the current channel.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        if (!guild.Encounters.ContainsKey(channelId))
        {
            await command.RespondAsync("No active encounter in this channel.", ephemeral: true);
            return;
        }

        // Give the user a confirm button
        EndEncounterMessage message = new()
        {
            ChannelId = (long)channelId,
            GuildId = (long)guildId,
            UserId = (long)command.User.Id
        };
        await dataService.AddMessageAsync(message, cancellationToken);

        ComponentBuilder builder = new();
        builder.WithButton("End encounter", message.GetMessageId());

        await command.RespondAsync($"Do you really want to end this encounter for all combatants?", ephemeral: true,
            components: builder.Build());
    }
}
