namespace ThirteenIsh.Commands.Pcs;

// TODO overly long subcommand name "encounter" -- change to "combat"?
internal sealed class PcEncounterSubCommandGroup() : SubCommandGroupBase("encounter", "Play in encounters.",
    new PcEncounterAttackCommand(),
    new PcEncounterJoinSubCommand(),
    new PcEncounterNextSubCommand())
{
}
