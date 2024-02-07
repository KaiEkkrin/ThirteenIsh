using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;

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

        // Respond with a character creation message
        // TODO work out how the responses come in, how to save etc
        var components = gameSystem.BuildCharacterEditor("TODO-CUSTOM-ID", null);
        await command.RespondAsync($"Creating character: {name}", ephemeral: true, components: components);
    }
}
