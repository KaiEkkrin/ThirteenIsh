using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterAddCommand() : SubCommandBase("add", "Adds a new character.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The character name.",
                isRequired: true)
            .AddOption(GameSystemBase.BuildGameSystemChoiceOption("game-system"));
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync("Character names must contain only letters and spaces", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "game-system", out var gameSystemName) ||
            GameSystemBase.AllGameSystems.FirstOrDefault(o => o.Name == gameSystemName) is not { } gameSystem)
        {
            await command.RespondAsync("Must choose a recognised game system", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.CreateCharacterAsync(name, gameSystemName, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync($"Error creating character '{name}'. Perhaps a character with that name already exists.",
                ephemeral: true);
            return;
        }

        await CommandUtil.RespondWithCharacterSheetAsync(command, character, $"Created character: {name}");
    }
}
