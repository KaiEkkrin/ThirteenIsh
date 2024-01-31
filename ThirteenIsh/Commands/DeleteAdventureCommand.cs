using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal class DeleteAdventureCommand : CommandBase
{
    public DeleteAdventureCommand() : base("adventure-delete", "Deletes an adventure")
    {
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        var builder = base.CreateBuilder();
        builder.AddOption("name", ApplicationCommandOptionType.String, "The adventure name",
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

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        if (!guild.Adventures.Any(o => o.Name == name))
        {
            await command.RespondAsync($"Cannot find an adventure named '{name}'. Perhaps it was already deleted?");
            return;
        }

        // I'm not going to delete this right away but instead give the user a confirm button
        var message = await dataService.CreateDeleteAdventureMessageAsync(name, guildId, command.User.Id, cancellationToken);

        ComponentBuilder builder = new();
        builder.WithButton("Delete", message.MessageId, ButtonStyle.Danger);

        await command.RespondAsync($"Do you really want to delete the adventure named '{name}'? This cannot be undone.",
            ephemeral: true, components: builder.Build());
    }
}
