namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcCommand() : CommandBase("pc", "Manage player characters in the current adventure.",
    new PcJoinSubCommand())
{
}
