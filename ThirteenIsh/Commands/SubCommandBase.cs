using Discord;

namespace ThirteenIsh.Commands;

/// <summary>
/// Implement sub-commands by extending this.
/// </summary>
internal abstract class SubCommandBase(string name, string description) : CommandOptionBase(name, description,
    ApplicationCommandOptionType.SubCommand)
{
}
