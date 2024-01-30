using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal sealed class CreateAdventureCommand : CommandBase
{
    public CreateAdventureCommand() : base("adventure-create", "Creates an adventure")
    {
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        var builder = base.CreateBuilder();
        builder.AddOption("name", ApplicationCommandOptionType.String, "The adventure name",
            isRequired: true);

        builder.AddOption("description", ApplicationCommandOptionType.String, "A description of the adventure");
        builder.WithDefaultMemberPermissions(GuildPermission.ManageGuild);
        return builder;
    }

    public override async Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;
        if (!TryGetCanonicalizedMultiPartOption(command.Data, "name", out var name))
        {
            await command.RespondAsync("Adventure names must contain only letters and spaces", ephemeral: true);
            return;
        }

        if (!TryGetOption<string>(command.Data, "description", out var description))
            description = string.Empty;

        // This will also make it the current adventure
        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.CreateAdventureAsync(name, description, guildId, cancellationToken);
        if (guild?.CurrentAdventure is null)
        {
            await command.RespondAsync($"Cannot create an adventure named '{name}'. Perhaps it was already created?",
                ephemeral: true);
            return;
        }

        await RespondWithAdventureSummaryAsync(command, guild.CurrentAdventure, $"Created adventure: {name}");
    }
}

