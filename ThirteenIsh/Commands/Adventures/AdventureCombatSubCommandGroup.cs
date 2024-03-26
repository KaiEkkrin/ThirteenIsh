namespace ThirteenIsh.Commands.Adventures;

internal class AdventureCombatSubCommandGroup() : SubCommandGroupBase("combat", "Manage encounters.",
    new AdventureCombatBeginCommand(),
    new AdventureCombatEndCommand())
{
}
