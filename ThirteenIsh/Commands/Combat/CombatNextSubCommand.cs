using Discord.WebSocket;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

/// <summary>
/// Moves to the next combatant in the encounter, i.e. the end-of-turn command.
/// I'm making this a command anyone can use because my players will be used to it from Avrae.
/// TODO Consider making the end-of-round command (as opposed to the end-of-turn command)
/// game master only (make it `adventure encounter next-turn` or something) because that one
/// is not going to be reversible and so trolling players could troll with the turn roll-over.
/// See this basic thing working first, though.
/// Also make a `prev` command to go to the previous combatant and a `swap` command to swap two
/// combatants in the initiative.
/// </summary>
internal sealed class CombatNextSubCommand() : SubCommandBase("next", "Moves on to the next combatant in the encounter.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new CombatNextMessage
        {
            ChannelId = channelId,
            GuildId = guildId,
            UserId = command.User.Id
        });
    }
}
