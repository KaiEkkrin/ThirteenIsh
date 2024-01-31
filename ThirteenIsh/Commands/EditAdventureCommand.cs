using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal sealed class EditAdventureCommand : CommandBase
{
    public EditAdventureCommand() : base("adventure-edit", "Edits an adventure")
    {
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        var builder = base.CreateBuilder();
        builder.AddOption("name", ApplicationCommandOptionType.String, "The adventure name",
            isRequired: true);

        builder.AddOption("description", ApplicationCommandOptionType.String, "A description of the adventure",
            isRequired: true);

        builder.WithDefaultMemberPermissions(GuildPermission.ManageGuild);
        return builder;
    }

    public override async Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(command.Data, "name", out var name))
        {
            await command.RespondAsync("Adventure names must contain only letters and spaces", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(command.Data, "description", out var description)) description = string.Empty;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EditAdventureAsync(name, description, guildId, cancellationToken);
        var adventure = guild?.Adventures.FirstOrDefault(o => o.Name == name);
        if (adventure is null)
        {
            await command.RespondAsync($"Cannot edit an adventure named '{name}'. Perhaps it does not exist?",
                ephemeral: true);
            return;
        }

        await CommandUtil.RespondWithAdventureSummaryAsync(command, adventure, adventure.Name);
    }
}
