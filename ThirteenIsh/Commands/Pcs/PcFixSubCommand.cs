using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcFixSubCommand(bool asGm) : SubCommandBase("fix", "Fixes a counter value for an adventurer.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOptionIf(asGm, builder => builder.AddOption("name", ApplicationCommandOptionType.String,
                "The character name.", isRequired: true))
            .AddOption("attribute", ApplicationCommandOptionType.String, "The attribute to fix.")
            .AddOption("value", ApplicationCommandOptionType.Integer, "The fix value.");
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

        if (!CommandUtil.TryGetOption<string>(option, "attribute", out var counterNamePart))
        {
            await command.RespondAsync("No attribute supplied.", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "value", out var fixValue))
        {
            await command.RespondAsync("No value supplied.", ephemeral: true);
            return;
        }

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new PcFixMessage
        {
            GuildId = guildId,
            Name = name,
            CounterNamePart = counterNamePart,
            FixValue = fixValue,
            UserId = command.User.Id
        });
    }
}
