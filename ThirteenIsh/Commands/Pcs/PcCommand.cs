namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcCommand() : CommandBase("pc", "Manage player characters in the current adventure.",
    new PcAttackSubCommand(),
    new PcFixSubCommand(false),
    new PcGetSubCommand(false),
    new PcJoinSubCommand(),
    new PcLeaveSubCommand(),
    new PcListSubCommand(false),
    new PcResetSubCommand(),
    new PcRollSubCommand(),
    new PcSetDefaultSubCommand(),
    new PcTagSubCommand(false),
    new PcUntagSubCommand(false),
    new PcUpdateSubCommand(),
    new PcVModSubCommand(false),
    new PcVSetSubCommand(false))
{
}
