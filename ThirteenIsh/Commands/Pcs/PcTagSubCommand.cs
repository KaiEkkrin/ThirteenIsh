using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcTagSubCommand(bool asGm) : SubCommandBase("tag", "Adds a tag to an adventurer.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String,
                asGm ? "The adventurer name." : "Your adventurer name (if you have multiple).",
                isRequired: false)
            .AddOption("tag", ApplicationCommandOptionType.String, "The tag to add", isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        string? name = null;
        CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out name);

        if (!CommandUtil.TryGetTagOption(option, "tag", out var tagValue))
        {
            await command.RespondAsync("A valid tag value is required.", ephemeral: true);
            return;
        }

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new PcTagMessage
        {
            GuildId = guildId,
            AsGm = asGm,
            Name = name,
            TagValue = tagValue,
            UserId = command.User.Id
        });
    }
}
