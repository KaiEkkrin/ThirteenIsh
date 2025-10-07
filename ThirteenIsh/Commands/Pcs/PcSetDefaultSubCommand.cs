using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcSetDefaultSubCommand() : SubCommandBase("setdefault", "Sets your default adventurer in the current adventure.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The adventurer name to set as default.", isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync("Please provide an adventurer name.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);
        var adventure = await dataService.GetAdventureAsync(guild, null, cancellationToken);
        if (adventure is null)
        {
            await command.RespondAsync("There is no current adventure.", ephemeral: true);
            return;
        }

        var result = await dataService.SetDefaultAdventurerAsync(adventure, command.User.Id, name, cancellationToken);
        if (result)
        {
            await command.RespondAsync($"Set '{name}' as your default adventurer.", ephemeral: true);
        }
        else
        {
            await command.RespondAsync($"Could not find an adventurer named '{name}' that you own in this adventure.", ephemeral: true);
        }
    }
}
