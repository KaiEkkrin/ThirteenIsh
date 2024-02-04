using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterAbilityRollSubCommand() : SubCommandBase("roll", "Rolls a check on a character's ability modifier.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddCharacterOption()
            .AddAbilityScoreOption()
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("bonus")
                .WithDescription("Any bonus dice to add.")
                .WithType(ApplicationCommandOptionType.String)
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

        var modifier = character.Sheet.GetAbilityModifier(name);
        BinaryOperationParseTree parseTree = new(0,
            new DiceRollParseTree(0, 1, 20),
            new IntegerParseTree(0, modifier, name),
            '+');

        if (CommandUtil.TryGetOption<string>(option, "bonus", out var bonusString))
        {
            var bonusParseTree = Parser.Parse(bonusString);
            if (!string.IsNullOrEmpty(bonusParseTree.Error))
            {
                await command.RespondAsync(bonusParseTree.Error, ephemeral: true);
                return;
            }

            if (bonusParseTree.Offset < bonusString.Length)
            {
                await command.RespondAsync($"Unrecognised input at end of string: '{bonusString[parseTree.Offset..]}'");
                return;
            }

            parseTree = new BinaryOperationParseTree(0, parseTree, bonusParseTree, '+');
        }

        var value = parseTree.Evaluate(out var working);

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle($"{characterName} made a {name} ({abilityScore}) check : {value}");
        embedBuilder.WithDescription(working);
        embedBuilder.WithCurrentTimestamp();

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
