using Discord;
using Discord.WebSocket;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

// Use `combat join` to join a combat as an adventurer -- done like that so only one
// copy of each adventurer can go into a combat (otherwise things would get very weird)!
internal sealed class CombatAddSubCommand() : SubCommandBase("add", "Adds a monster to the current encounter.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The monster name.", isRequired: true)
            .AddOption("count", ApplicationCommandOptionType.Integer, "The number of monsters to swarm together.")
            .AddRerollsOption("rerolls");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync(
                "Monster names must contain only letters and spaces", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "count", out var count)) count = 1;
        if (!CommandUtil.TryGetOption<int>(option, "rerolls", out var rerolls)) rerolls = 0;

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new CombatAddMessage
        {
            ChannelId = channelId,
            GuildId = guildId,
            Name = name,
            Rerolls = rerolls,
            SwarmCount = count,
            UserId = command.User.Id
        });
    }
}
