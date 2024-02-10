using Discord;
using Discord.WebSocket;
using ThirteenIsh.Commands;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Entities.Messages;

public class EditCharacterMessage : MessageBase
{
    public const string PropertyGroupControlId = "PropertyGroup";
    public const string PropertyControlId = "Property";
    public const string ValueControlId = "Value";
    public const string CancelControlId = "Cancel";

    /// <summary>
    /// The character name to edit.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The property group name to pick from.
    /// </summary>
    public string? PropertyGroupName { get; set; }

    /// <summary>
    /// The property name to edit.
    /// TODO try to make it so that instead I see dropdowns for every property in the group?
    /// </summary>
    public string? PropertyName { get; set; }

    public override async Task<bool> HandleAsync(SocketMessageComponent component, string controlId,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        if (controlId == CancelControlId)
        {
            await component.RespondAsync("Edit cancelled.", ephemeral: true);
            return true;
        }

        if (component.Data.Values.FirstOrDefault() is not { Length: > 0 } selectionValue)
        {
            await component.RespondAsync("No selection provided.", ephemeral: true);
            return true;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(Name, component.User.Id, cancellationToken);
        if (character is null)
        {
            await component.RespondAsync($"Cannot find a character named '{Name}'. Perhaps they were deleted?",
                ephemeral: true);
            return true;
        }

        var gameSystem = GameSystem.Get(character.GameSystem);
        if (gameSystem is null)
        {
            await component.RespondAsync($"Cannot find a game system named '{character.GameSystem}'.",
                ephemeral: true);
            return true;
        }

        if (PropertyGroupName is null)
        {
            EditCharacterMessage message = new()
            {
                Name = Name,
                PropertyGroupName = selectionValue,
                UserId = (long)component.User.Id
            };
            await dataService.AddMessageAsync(message, cancellationToken);

            var selectMenuBuilder = gameSystem.BuildPropertyChoiceComponent(message.GetMessageId(PropertyControlId),
                property => property.CanStore, selectionValue);
            if (selectMenuBuilder.Options.Count == 0)
            {
                await component.RespondAsync(
                    $"Cannot find any property selections for '{PropertyGroupName}' in {character.GameSystem}.");
                return true;
            }

            var componentBuilder = new ComponentBuilder().WithSelectMenu(selectMenuBuilder)
                .WithButton("Cancel", message.GetMessageId(CancelControlId));

            await component.RespondAsync($"Editing '{Name}' : Select a property to change", ephemeral: true,
                components: componentBuilder.Build());

            return true;
        }
        else if (PropertyName is null)
        {
            // Send the user the value select menu
            // TODO also provide a cancel button here
            EditCharacterMessage message = new()
            {
                Name = Name,
                PropertyGroupName = PropertyGroupName,
                PropertyName = selectionValue,
                UserId = (long)component.User.Id
            };
            await dataService.AddMessageAsync(message, cancellationToken);

            if (!gameSystem.TryBuildPropertyValueChoiceComponent(message.GetMessageId(ValueControlId),
                selectionValue, character.Sheet, out var menuBuilder, out var errorMessage))
            {
                await component.RespondAsync(errorMessage, ephemeral: true);
                return true;
            }

            var componentBuilder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder)
                .WithButton("Cancel", message.GetMessageId(CancelControlId));

            await component.RespondAsync($"Editing '{Name}' : Select a '{selectionValue}' value", ephemeral: true,
                components: componentBuilder.Build());

            return true;
        }
        else
        {
            // Save the change to the character
            try
            {
                character = await dataService.UpdateCharacterAsync(Name,
                    sheet => gameSystem.EditCharacterProperty(PropertyName, selectionValue, sheet),
                    component.User.Id, cancellationToken);

                await CommandUtil.RespondWithCharacterSheetAsync(component, character, $"Edited '{Name}'",
                    PropertyName);
            }
            catch (GamePropertyException ex)
            {
                await component.RespondAsync(ex.Message, ephemeral: true);
            }

            return true;
        }
    }
}
