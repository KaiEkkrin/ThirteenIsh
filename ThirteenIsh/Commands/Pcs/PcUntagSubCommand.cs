using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcUntagSubCommand(bool asGm) : SubCommandBase("untag", "Removes a tag from an adventurer.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOptionIf(asGm, builder => builder.AddOption("name", ApplicationCommandOptionType.String,
                "The character name.", isRequired: true))
            .AddOption("tag", ApplicationCommandOptionType.String, "The tag to remove", isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        string? name = null;
        if (asGm && !CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out name))
        {
            await command.RespondAsync(
                $"A valid {CharacterType.PlayerCharacter.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter)} name must be supplied.",
                ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetTagOption(option, "tag", out var tagValue))
        {
            await command.RespondAsync("A valid tag value is required.", ephemeral: true);
            return;
        }

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new PcUntagMessage
        {
            GuildId = guildId,
            Name = name,
            TagValue = tagValue,
            UserId = command.User.Id
        });
    }
}
