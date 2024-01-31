using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterAbilitySetSubCommand() : SubCommandBase("set", "Sets a character's ability score.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddCharacterOption()
            .AddAbilityScoreOption()
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("value")
                .WithDescription("The new ability score value.")
                .WithMaxValue(20)
                .WithMinValue(1)
                .WithType(ApplicationCommandOptionType.Integer)
                );
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "character", out var characterName))
        {
            await command.RespondAsync("Character not found", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetCanonicalizedOption(option, "name", out var name) ||
            !AttributeName.AbilityScores.Contains(name))
        {
            await command.RespondAsync("No such ability score", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "value", out var abilityScore))
        {
            await command.RespondAsync("No value provided", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.UpdateCharacterAsync(characterName, UpdateSheet, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync($"Cannot edit a character named '{characterName}'. Perhaps they do not exist?",
                ephemeral: true);
            return;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle($"Edited {name} score for {characterName}");
        embedBuilder.AddField(new EmbedFieldBuilder()
            .WithIsInline(true)
            .WithName("Score")
            .WithValue(abilityScore));

        embedBuilder.AddField(new EmbedFieldBuilder()
            .WithIsInline(true)
            .WithName("Modifier")
            .WithValue(character.Sheet.GetAbilityModifier(name)));

        await command.RespondAsync(embed: embedBuilder.Build());

        void UpdateSheet(CharacterSheet sheet)
        {
            sheet.AbilityScores[name] = abilityScore;
        }
    }
}
