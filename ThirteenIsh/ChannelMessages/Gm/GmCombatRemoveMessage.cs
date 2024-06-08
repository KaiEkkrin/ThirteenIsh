using ThirteenIsh.ChannelMessages.Combat;

namespace ThirteenIsh.ChannelMessages.Gm;

internal sealed class GmCombatRemoveMessage : CombatMessage
{
    public required string Alias { get; init; }
}
