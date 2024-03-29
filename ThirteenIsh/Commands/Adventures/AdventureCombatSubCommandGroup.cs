namespace ThirteenIsh.Commands.Adventures;

/// <summary>
/// GM-only combat commands go here. I'm going to try to make this list as short as I can,
/// since allowing players to do various things in combat (e.g. add monsters, for their own
/// minions) is useful...
/// </summary>
internal class AdventureCombatSubCommandGroup() : SubCommandGroupBase("combat", "Manage encounters.",
    new AdventureCombatBeginCommand(),
    new AdventureCombatEndCommand())
{
}
