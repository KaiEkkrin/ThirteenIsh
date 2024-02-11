using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcResetSubCommand() : SubCommandBase("reset", "Resets the current adventurer's variables.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        if (guild.CurrentAdventure is null)
        {
            await command.RespondAsync($"There is no current adventure in this guild.");
            return;
        }

        if (!guild.CurrentAdventure.Adventurers.TryGetValue(command.User.Id, out var adventurer))
        {
            await command.RespondAsync("You do not have a character in the current adventure.", ephemeral: true);
            return;
        }

        // Supply a confirm button
        ResetAdventurerMessage message = new()
        {
            GuildId = (long)guildId,
            AdventureName = guild.CurrentAdventure.Name,
            UserId = (long)command.User.Id
        };

        await dataService.AddMessageAsync(message, cancellationToken);

        ComponentBuilder builder = new();
        builder.WithButton("Reset", message.GetMessageId(), ButtonStyle.Primary);

        await command.RespondAsync(
            $"Do you really want to reset all the variables of the adventurer named '{adventurer.Name}'?",
            ephemeral: true, components: builder.Build());
    }
}
