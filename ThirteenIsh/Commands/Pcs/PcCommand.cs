﻿namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcCommand() : CommandBase("pc", "Manage player characters in the current adventure.",
    new PcGetSubCommand(),
    new PcEncounterSubCommandGroup(),
    new PcJoinSubCommand(),
    new PcLeaveSubCommand(),
    new PcResetSubCommand(),
    new PcRollSubCommand(),
    new PcUpdateSubCommand(),
    new PcVModSubCommand(),
    new PcVSetSubCommand())
{
}
