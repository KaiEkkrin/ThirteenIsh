using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Entities.Messages;
using ThirteenIsh.Services;
using CharacterType = ThirteenIsh.Database.Entities.CharacterType;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterRemoveSubCommand(CharacterType characterType)
    : SubCommandBase("remove", $"Deletes a {characterType.FriendlyName()}.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, $"The {characterType.FriendlyName()} name.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync(
                $"{characterType.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter)} names must contain only letters and spaces",
                ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, characterType, cancellationToken);
        if (character == null)
        {
            await command.RespondAsync(
                $"Cannot find a {characterType.FriendlyName()} named '{name}'. Perhaps they were already deleted?");
            return;
        }

        // I'm not going to delete this right away but instead give the user a confirm button
        DeleteCharacterMessage message = new()
        {
            Name = name,
            UserId = (long)command.User.Id
        };
        await dataService.AddMessageAsync(message, cancellationToken);

        var builder = new ComponentBuilder()
            .WithButton("Delete", message.GetMessageId(), ButtonStyle.Danger);

        await command.RespondAsync(
            $"Do you really want to delete the {characterType.FriendlyName()} named '{name}'? This cannot be undone.",
            ephemeral: true, components: builder.Build());
    }
}
