namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcEncounterSubCommandGroup() : SubCommandGroupBase("encounter", "Play in encounters.",
    new PcEncounterJoinSubCommand(),
    new PcEncounterNextSubCommand())
{
}
