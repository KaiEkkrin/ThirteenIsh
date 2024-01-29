using Discord;
using Discord.WebSocket;
using ThirteenIsh.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal sealed class DeleteCharacterCommand : CommandBase
{
    public DeleteCharacterCommand() : base("delete-character", "Deletes a character")
    {
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        var builder = base.CreateBuilder();
        builder.AddOption("name", ApplicationCommandOptionType.String, "The character name",
            isRequired: true);

        return builder;
    }

    public override async Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!TryGetCanonicalizedMultiPartOption(command.Data, "name", out var name))
        {
            await command.RespondAsync("Character names must contain only letters and spaces", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync($"Cannot find a character named '{name}'. Perhaps they were already deleted?",
                ephemeral: true);
            return;
        }

        // I'm not going to delete this right away but instead give the user a confirm button
        DeleteCharacterMessage message = new(name, command.User.Id);

        var discordService = serviceProvider.GetRequiredService<DiscordService>();
        discordService.AddMessageInteraction(message);

        ComponentBuilder builder = new();
        builder.WithButton("Delete", message.MessageId, ButtonStyle.Danger);

        await command.RespondAsync($"Do you really want to delete the character named '{name}'? This cannot be undone.",
            ephemeral: true, components: builder.Build());
    }
}
