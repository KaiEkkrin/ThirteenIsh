using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Combat;

[MessageHandler(MessageType = typeof(CombatVModMessage))]
internal sealed class CombatVModMessageHandler(SqlDataService dataService, DiscordService discordService,
    IRandomWrapper random, IServiceProvider serviceProvider)
    : CombatVSubMessageHandlerBase<CombatVModMessage>(dataService, discordService, random, serviceProvider)
{
    protected override CombatEditVariableOperation CreateEditOperation(CombatVModMessage message)
    {
        return new CombatEditVariableOperation(new ModVariableSubOperation(message.VariableNamePart, message.DiceParseTree,
            Random));
    }
}
