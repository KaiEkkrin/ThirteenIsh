namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcCommand() : CommandBase("pc", "Manage player characters in the current adventure.",
    new PcGetSubCommand(),
    new PcJoinSubCommand(),
    new PcLeaveSubCommand(),
    new PcUpdateSubCommand())
{
}
