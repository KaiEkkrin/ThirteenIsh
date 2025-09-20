namespace ThirteenIsh.Commands.Gm;

/// <summary>
/// GM role management commands go here.
/// </summary>
internal class GmRoleSubCommandGroup() : SubCommandGroupBase("role", "Manage GM role permissions.",
    new GmRoleGetSubCommand(),
    new GmRoleSetSubCommand())
{
}