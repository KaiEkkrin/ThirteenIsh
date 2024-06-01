using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

/// <summary>
/// Adds a custom counter to a character.
/// </summary>
internal sealed class CharacterCcAddSubCommand(CharacterType characterType)
    : SubCommandBase("add", $"Adds a custom counter to a {characterType.FriendlyName()}.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, $"The {characterType.FriendlyName()} name.",
                isRequired: true)
            .AddOption("cc-name", ApplicationCommandOptionType.String, "The custom counter name to add.",
                isRequired: true)
            .AddOption("value", ApplicationCommandOptionType.Integer, "The default value.", isRequired: true)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("type")
                .WithDescription("The type of custom counter.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer)
                .AddChoice("Rollable", (int)GameCounterOptions.CanRoll)
                .AddChoice("Variable", (int)GameCounterOptions.HasVariable));
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

        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "cc-name", out var ccName))
        {
            await command.RespondAsync("A counter name is required.", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "value", out var defaultValue))
        {
            await command.RespondAsync("A default value is required.", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "type", out var typeInt))
        {
            await command.RespondAsync("A type selection is required.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, characterType, 
            cancellationToken: cancellationToken);
        if (character is null)
        {
            await command.RespondAsync(
                $"Error getting {characterType.FriendlyName()} '{name}'. Perhaps they do not exist, or there is more than one character or monster matching that name?",
                ephemeral: true);
            return;
        }

        var result = await dataService.EditCharacterAsync(
            name, new EditOperation(ccName, defaultValue, (GameCounterOptions)typeInt), command.User.Id,
            characterType, cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            updatedCharacter =>
            {
                return CommandUtil.RespondWithCharacterSheetAsync(command, updatedCharacter,
                    $"Edited {characterType.FriendlyName()} '{updatedCharacter.Name}'", [ccName]);
            });
    }

    private sealed class EditOperation(string ccName, int defaultValue, GameCounterOptions options)
        : SyncEditOperation<Database.Entities.Character, Database.Entities.Character>
    {
        public override EditResult<Database.Entities.Character> DoEdit(DataContext context,
            Database.Entities.Character character)
        {
            var existingCc = character.Sheet.CustomCounters
                ?.FirstOrDefault(cc => cc.Name.Equals(ccName, StringComparison.OrdinalIgnoreCase));

            if (existingCc != null)
            {
                return CreateError(
                    $"The character '{character.Name}' already has a custom counter named '{existingCc.Name}'");
            }

            character.Sheet.CustomCounters ??= [];
            character.Sheet.CustomCounters.Add(new CustomCounter(ccName, defaultValue, options));
            return new EditResult<Database.Entities.Character>(character);
        }
    }
}
