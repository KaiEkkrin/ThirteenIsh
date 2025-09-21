using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Pcs;

[MessageHandler(MessageType = typeof(PcVModMessage))]
internal sealed class PcVModMessageHandler(SqlDataService dataService, IRandomWrapper random)
    : PcVSubMessageHandlerBase<PcVModMessage>(dataService, random)
{
    protected override PcEditVariableOperation CreateEditOperation(PcVModMessage message)
    {
        return new PcEditVariableOperation(new ModVariableSubOperation(message.VariableNamePart, message.DiceParseTree, Random));
    }
}
