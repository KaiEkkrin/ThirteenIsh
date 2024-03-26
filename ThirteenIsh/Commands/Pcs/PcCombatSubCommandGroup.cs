namespace ThirteenIsh.Commands.Pcs;

// TODO overly long subcommand name "encounter" -- change to "combat"?
internal sealed class PcCombatSubCommandGroup() : SubCommandGroupBase("combat", "Play in encounters.",
    new PcCombatAttackCommand(),
    new PcCombatDamageCommand(),
    new PcCombatJoinSubCommand(),
    new PcCombatNextSubCommand())
{
}
