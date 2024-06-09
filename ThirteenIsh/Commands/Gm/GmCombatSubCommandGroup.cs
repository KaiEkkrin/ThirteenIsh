using ThirteenIsh.Commands.Combat;

namespace ThirteenIsh.Commands.Gm;

/// <summary>
/// GM-only combat commands go here. I'm going to try to make this list as short as I can,
/// since allowing players to do various things in combat (e.g. add monsters, for their own
/// minions) is useful...
/// TODO add a `gm combat get` command for getting the details of a combatant (remember the
/// response should be ephemeral so players don't see it)
/// </summary>
internal class GmCombatSubCommandGroup() : SubCommandGroupBase("combat", "Manage encounters.",
    new GmCombatBeginSubCommand(),
    new GmCombatEndSubCommand(),
    new GmCombatRemoveSubCommand(),
    new GmCombatSwitchCommand(),
    new CombatTagSubCommand(true),
    new CombatUntagSubCommand(true),
    new CombatVModSubCommand(true),
    new CombatVSetSubCommand(true))
{
}
