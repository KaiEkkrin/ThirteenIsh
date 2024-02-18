namespace ThirteenIsh.Commands.Adventures;

internal class AdventureEncounterSubCommandGroup() : SubCommandGroupBase("encounter", "Manage encounters.",
    new AdventureEncounterBeginCommand(),
    new AdventureEncounterEndCommand())
{
}
