using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

/// <summary>
/// Extend this to make the vset and vmod commands since they're extremely similar
/// These, of course, don't apply to monsters, which don't have an equivalent to "player character" sheets
/// copied into the adventure
/// </summary>
internal abstract class PcVSubCommandBase(bool asGm, string name, string description,
    string nameOptionDescription, string valueOptionDescription)
    : SubCommandBase(name, description)
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOptionIf(asGm, builder => builder.AddOption("name", ApplicationCommandOptionType.String,
                "The character name.", isRequired: true))
            .AddOption("variable-name", ApplicationCommandOptionType.String, nameOptionDescription)
            .AddOption("value", ApplicationCommandOptionType.String, valueOptionDescription);
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

        if (!CommandUtil.TryGetOption<string>(option, "variable-name", out var variableNamePart))
        {
            await command.RespondAsync("No variable name part supplied.", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "value", out var diceString))
        {
            await command.RespondAsync("No value supplied.", ephemeral: true);
            return;
        }

        var parseTree = Parser.Parse(diceString);
        if (!string.IsNullOrEmpty(parseTree.Error))
        {
            await command.RespondAsync(parseTree.Error, ephemeral: true);
            return;
        }

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command,
            BuildMessage(guildId, command.User.Id, name, variableNamePart, parseTree));
    }

    protected abstract PcVSubMessageBase BuildMessage(ulong guildId, ulong userId, string? name, string variableNamePart,
        ParseTreeBase diceParseTree);
}
