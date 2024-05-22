using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterCcRemoveSubCommand(CharacterType characterType)
    : SubCommandBase("remove", $"Deletes a custom counter from a {characterType.FriendlyName()}.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, $"The {characterType.FriendlyName()} name.",
                isRequired: true)
            .AddOption("cc-name", ApplicationCommandOptionType.String, "The custom counter name to delete.",
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

        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "cc-name", out var ccName))
        {
            await command.RespondAsync("A counter name is required.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, characterType, 
            cancellationToken: cancellationToken);
        if (character == null)
        {
            await command.RespondAsync(
                $"Cannot find a {characterType.FriendlyName()} named '{name}'. Perhaps they were deleted?");
            return;
        }

        DeleteCustomCounterMessage message = new()
        {
            CcName = ccName,
            CharacterType = characterType,
            Name = name,
            UserId = command.User.Id
        };
        await dataService.AddMessageAsync(message, cancellationToken);

        var builder = new ComponentBuilder()
            .WithButton("Delete", message.GetMessageId(), ButtonStyle.Danger);

        await command.RespondAsync(
            $"Do you really want to delete the custom counter '{ccName}' from the {characterType.FriendlyName()} named '{name}'? This cannot be undone.",
            ephemeral: true, components: builder.Build());
    }
}
