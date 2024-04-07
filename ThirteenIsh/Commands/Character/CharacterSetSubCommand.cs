using Discord;
using Discord.WebSocket;
using ThirteenIsh;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Game;
using ThirteenIsh.Services;
using CharacterType = ThirteenIsh.Database.Entities.CharacterType;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterSetSubCommand(CharacterType characterType)
    : SubCommandBase("set", $"Sets a property for a {characterType.FriendlyName()}.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, $"The {characterType.FriendlyName()} name.",
                isRequired: true)
            .AddOption("property-name", ApplicationCommandOptionType.String, "The property name to set.",
                isRequired: true)
            .AddOption("value", ApplicationCommandOptionType.String, "The property value.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync(
                $"{characterType.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter)} names must contain only letters and spaces", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "property-name", out var propertyName))
        {
            await command.RespondAsync("A property name is required.", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "value", out var newValue))
        {
            await command.RespondAsync("A value is required.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, characterType, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync(
                $"Error getting {characterType.FriendlyName()} '{name}'. Perhaps they do not exist, or there is more than one character or monster matching that name?",
                ephemeral: true);
            return;
        }

        var gameSystem = GameSystem.Get(character.GameSystem);
        var characterSystem = gameSystem.GetCharacterSystem(characterType);
        var property = characterSystem.FindStorableProperty(propertyName);
        if (property is null)
        {
            await command.RespondAsync($"'{propertyName}' does not uniquely match a settable property name.",
                ephemeral: true);
            return;
        }

        var (updatedCharacter, errorMessage) = await dataService.EditCharacterAsync(
            name, new SetCharacterPropertyOperation(property, newValue), command.User.Id, characterType,
            cancellationToken);

        if (errorMessage is not null)
        {
            await command.RespondAsync(errorMessage, ephemeral: true);
            return;
        }

        if (updatedCharacter is null) throw new InvalidOperationException(
            $"{characterType.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter)} object was null after update");

        await CommandUtil.RespondWithCharacterSheetAsync(command, updatedCharacter,
            $"Edited {characterType.FriendlyName()} '{updatedCharacter.Name}'", property.Name);
    }
}
