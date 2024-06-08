using Discord;
using Discord.WebSocket;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

/// <summary>
/// Like PcVSubCommandBase, but applies to any alias during combat.
/// </summary>
internal abstract class CombatVSubCommandBase(bool asGm, string name, string description, string nameOptionDescription,
    string valueOptionDescription)
    : SubCommandBase(name, description)
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("alias", ApplicationCommandOptionType.String, "The combatant alias to edit.",
                isRequired: asGm)
            .AddOption("variable-name", ApplicationCommandOptionType.String, nameOptionDescription,
                isRequired: true)
            .AddOption("value", ApplicationCommandOptionType.String, valueOptionDescription,
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;
        if (!CommandUtil.TryGetOption<string>(option, "variable-name", out var namePart))
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

        var alias = CommandUtil.TryGetOption<string>(option, "alias", out var aliasString)
            ? aliasString
            : null;

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command,
            BuildMessage(guildId, channelId, command.User.Id, asGm, alias, namePart, parseTree));
    }

    protected abstract CombatVSubMessageBase BuildMessage(ulong guildId, ulong channelId, ulong userId, bool asGm, string? alias,
        string variableNamePart, ParseTreeBase diceParseTree);
}
