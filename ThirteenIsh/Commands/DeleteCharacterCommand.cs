using Discord;
using Discord.WebSocket;
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

        // TODO instead give the user a red confirm button
        var dataService = serviceProvider.GetRequiredService<DataService>();
        if (!await dataService.DeleteCharacterAsync(name, command.User.Id, cancellationToken))
        {
            await command.RespondAsync($"Cannot delete a character named '{name}'. Perhaps they do not exist?",
                ephemeral: true);
            return;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle($"Deleted character: {name}");

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
