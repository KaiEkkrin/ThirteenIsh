using ThirteenIsh.ChannelMessages;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Combat;

internal sealed class CombatVModSubCommand(bool asGm) : CombatVSubCommandBase(asGm, "vmod",
    "Adds to or subtracts from a variable value,",
    "The variable name to change.", "A number or dice expression to change it by.")
{
    protected override CombatVSubMessageBase BuildMessage(ulong guildId, ulong channelId, ulong userId, bool asGm,
        string? alias, string variableNamePart, ParseTreeBase diceParseTree)
    {
        return new CombatVModMessage()
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
