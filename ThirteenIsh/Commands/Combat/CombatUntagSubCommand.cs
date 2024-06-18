using Discord;
using Discord.WebSocket;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

internal sealed class CombatUntagSubCommand(bool asGm) : SubCommandBase("untag", "Removes a tag from a combatant.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("alias", ApplicationCommandOptionType.String, "The combatant alias to edit.",
                isRequired: asGm)
            .AddOption("tag", ApplicationCommandOptionType.String, "The tag to remove", isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var alias = CommandUtil.TryGetOption<string>(option, "alias", out var aliasString)
            ? aliasString
            : null;

        if (!CommandUtil.TryGetTagOption(option, "tag", out var tagValue))
        {
            await command.RespondAsync("A valid tag value is required.", ephemeral: true);
            return;
        }

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new CombatUntagMessage
        {
            GuildId = guildId,
            ChannelId = channelId,
            AsGm = asGm,
            Alias = alias,
            TagValue = tagValue,
            UserId = command.User.Id
        });
    }
}
