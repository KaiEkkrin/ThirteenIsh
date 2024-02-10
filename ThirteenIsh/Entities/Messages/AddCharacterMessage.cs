using Discord;
using Discord.WebSocket;
using ThirteenIsh.Commands;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Entities.Messages;

/// <summary>
/// For adding a character. Forms a wizard that walks you through the character
/// properties group by group.
/// </summary>
public class AddCharacterMessage : MessageBase
{
    public const string CancelControlId = "Cancel";
    public const string DoneControlId = "Done";

    /// <summary>
    /// The character name to edit.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The property group name we are editing.
    /// </summary>
    public string PropertyGroupName { get; set; } = string.Empty;

    public override async Task<bool> HandleAsync(SocketMessageComponent component, string controlId,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        if (controlId == CancelControlId)
        {
            await component.RespondAsync(
                "Add cancelled. The character has already been saved; you can continue editing them with the `character edit` command, or delete them with `character remove.`",
                ephemeral: true);
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

        if (controlId == DoneControlId)
        {
            return await NextPropertyGroupAsync(component, character, dataService, gameSystem, cancellationToken);
        }

        return await EditPropertyAsync(component, dataService, gameSystem, controlId, cancellationToken);
    }

    private async Task<bool> EditPropertyAsync(
        SocketMessageComponent component,
        DataService dataService,
        GameSystem gameSystem,
        string propertyName,
        CancellationToken cancellationToken)
    {
        var newValue = component.Data.Values.FirstOrDefault();
        if (newValue is null) return false;

        try
        {
            await dataService.UpdateCharacterAsync(
                Name,
                sheet => gameSystem.EditCharacterProperty(propertyName, newValue, sheet),
                component.User.Id,
                cancellationToken);

            await component.DeferAsync(true);
            return false; // keep this message around, the user might make more selections
        }
        catch (GamePropertyException ex)
        {
            await component.RespondAsync(ex.Message, ephemeral: true);
            return true;
        }
    }

    private async Task<bool> NextPropertyGroupAsync(
        SocketMessageComponent component,
        Character character,
        DataService dataService,
        GameSystem gameSystem,
        CancellationToken cancellationToken)
    {
        var propertyGroupIndex = gameSystem.PropertyGroups.FindIndex(group => group.GroupName == PropertyGroupName);
        var nextPropertyGroup = gameSystem.PropertyGroups.ElementAtOrDefault(propertyGroupIndex + 1);
        if (nextPropertyGroup is null)
        {
            // Edit completed
            await CommandUtil.RespondWithCharacterSheetAsync(component, character, $"Added '{Name}'");
            return true;
        }

        // Build and move to the next page
        AddCharacterMessage message = new()
        {
            Name = Name,
            PropertyGroupName = nextPropertyGroup.GroupName,
            UserId = (long)component.User.Id
        };
        await dataService.AddMessageAsync(message, cancellationToken);
        await message.RespondWithWizardPageAsync(component, character, gameSystem);
        return true;
    }

    internal async Task RespondWithWizardPageAsync(
        IDiscordInteraction interaction,
        Character character,
        GameSystem gameSystem)
    {
        var propertyGroup = gameSystem.PropertyGroups.FirstOrDefault(group => group.GroupName == PropertyGroupName);
        if (propertyGroup is null)
        {
            await interaction.RespondAsync($"Cannot find a property group '{PropertyGroupName}' in {gameSystem.Name}.",
                ephemeral: true);
            return;
        }

        // TODO This doesn't work, because I can (apparently) only have 5 rows in one of these responses :/
        // Try to expand the wizard to paginate through each property group 4-at-a-time. I expect I'll need to
        // create a separate class PropertyWizard containing the relevant things. Use 4 rows for select menus (one
        // per row) and a fifth row for the Done and Cancel buttons (rename Done to Next?)
        ComponentBuilder componentBuilder = new();
        foreach (var property in propertyGroup.GetProperties(property => property.CanStore))
        {
            if (!gameSystem.TryBuildPropertyValueChoiceComponent(
                GetMessageId(property.Name), property.Name, character.Sheet, out var menuBuilder, out var errorMessage))
            {
                // The rest of the logic assumes this won't happen
                throw new InvalidOperationException(errorMessage);
            }

            componentBuilder.WithSelectMenu(menuBuilder);
        }

        componentBuilder.WithButton("Done", GetMessageId(DoneControlId))
            .WithButton("Cancel", GetMessageId(CancelControlId), ButtonStyle.Secondary);

        await interaction.RespondAsync($"Adding '{Name}' : Set {PropertyGroupName}", ephemeral: true,
            components: componentBuilder.Build());
    }
}
