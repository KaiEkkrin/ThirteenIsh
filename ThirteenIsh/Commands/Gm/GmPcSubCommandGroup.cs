using ThirteenIsh.Commands.Pcs;

namespace ThirteenIsh.Commands.Gm;

/// <summary>
/// GM-only PC commands go here.
/// </summary>
internal class GmPcSubCommandGroup() : SubCommandGroupBase("pc", "Manage player characters.",
    new PcFixSubCommand(true),
    new PcGetSubCommand(true),
    new PcTagSubCommand(true),
    new PcUntagSubCommand(true),
    new PcVModSubCommand(true),
    new PcVSetSubCommand(true))
{
}
