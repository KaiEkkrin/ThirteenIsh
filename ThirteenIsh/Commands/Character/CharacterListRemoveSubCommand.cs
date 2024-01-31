using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal class CharacterListRemoveSubCommand() : SubCommandBase("remove", "Deletes a character.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddCharacterOption("name");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
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
        var message = await dataService.CreateDeleteCharacterMessageAsync(name, command.User.Id, cancellationToken);

        ComponentBuilder builder = new();
        builder.WithButton("Delete", message.MessageId, ButtonStyle.Danger);

        await command.RespondAsync($"Do you really want to delete the character named '{name}'? This cannot be undone.",
            ephemeral: true, components: builder.Build());
    }
}
