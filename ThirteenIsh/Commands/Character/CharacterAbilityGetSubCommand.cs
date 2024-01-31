using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterAbilityGetSubCommand() : SubCommandBase("get", "Gets a character's ability score.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddCharacterOption()
            .AddAbilityScoreOption();
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "character", out var characterName))
        {
            await command.RespondAsync("Character not found", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(characterName, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync("Character not found", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetCanonicalizedOption(option, "name", out var name) ||
            !character.Sheet.AbilityScores.TryGetValue(name, out var abilityScore))
        {
            await command.RespondAsync("No such ability score", ephemeral: true);
            return;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle($"{name} score for {characterName}");
        embedBuilder.AddField(new EmbedFieldBuilder()
            .WithIsInline(true)
            .WithName("Score")
            .WithValue(abilityScore));

        embedBuilder.AddField(new EmbedFieldBuilder()
            .WithIsInline(true)
            .WithName("Modifier")
            .WithValue(character.Sheet.GetAbilityModifier(name)));

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
