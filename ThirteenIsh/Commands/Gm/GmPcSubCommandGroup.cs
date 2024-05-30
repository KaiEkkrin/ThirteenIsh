using ThirteenIsh.Commands.Pcs;

namespace ThirteenIsh.Commands.Gm;

/// <summary>
/// GM-only PC commands go here.
/// </summary>
internal class GmPcSubCommandGroup() : SubCommandGroupBase("pc", "Manage player characters.",
    new PcGetSubCommand(true))
{
}
