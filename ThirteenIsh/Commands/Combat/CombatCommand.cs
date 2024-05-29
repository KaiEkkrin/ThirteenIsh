namespace ThirteenIsh.Commands.Combat;

/// <summary>
/// General and monster-related combat commands go here.
/// </summary>
internal sealed class CombatCommand() : CommandBase("combat", "Play in encounters.",
    new CombatAddSubCommand(),
    new CombatAttackSubCommand(),
    new CombatDamageSubCommand(),
    new CombatJoinSubCommand(),
    new CombatNextSubCommand(),
    new CombatRollSubCommand(),
    new CombatVModSubCommand(),
    new CombatVSetSubCommand())
{
}
