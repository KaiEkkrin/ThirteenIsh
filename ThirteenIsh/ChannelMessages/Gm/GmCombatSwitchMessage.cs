using ThirteenIsh.ChannelMessages.Combat;

namespace ThirteenIsh.ChannelMessages.Gm;

internal sealed class GmCombatSwitchMessage : CombatMessage
{
    public required string Alias { get; init; }
}
