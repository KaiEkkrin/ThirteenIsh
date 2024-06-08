using ThirteenIsh.ChannelMessages;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Combat;

internal sealed class CombatVSetSubCommand(bool asGm) : CombatVSubCommandBase(asGm, "vset", "Sets a variable value.",
    "The variable name to set.", "A number or dice expression to set it to.")
{
    protected override CombatVSubMessageBase BuildMessage(ulong guildId, ulong channelId, ulong userId, bool asGm,
        string? alias, string variableNamePart, ParseTreeBase diceParseTree)
    {
        return new CombatVSetMessage()
        {
            GuildId = guildId,
            ChannelId = channelId,
            UserId = userId,
            AsGm = asGm,
            Alias = alias,
            VariableNamePart = variableNamePart,
            DiceParseTree = diceParseTree
        };
    }
}
