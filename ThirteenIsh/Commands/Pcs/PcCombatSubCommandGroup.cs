namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcCombatSubCommandGroup() : SubCommandGroupBase("combat", "Play in encounters.",
    new PcCombatAttackCommand(),
    new PcCombatDamageCommand(),
    new PcCombatJoinSubCommand(),
    new PcCombatNextSubCommand())
{
}
