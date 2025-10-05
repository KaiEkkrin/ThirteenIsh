using ThirteenIsh.Parsing;

namespace ThirteenIsh.ChannelMessages.Combat;

internal sealed class CombatAttackMessage : CombatMessage
{
    public string? Alias { get; init; }
    public required string NamePart { get; init; }
    public ParseTreeBase? Bonus { get; init; }
    public int Rerolls { get; init; }
    public IReadOnlyCollection<string>? Targets { get; init; }
    public string? VsNamePart { get; init; }
    public string? SecondaryNamePart { get; init; }
}
