using Discord;
using Discord.WebSocket;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;

namespace ThirteenIsh.Commands;

internal sealed class CreateCharacterCommand : CommandBase
{
    public CreateCharacterCommand() : base("create-character", "Creates a character")
    {
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        var builder = base.CreateBuilder();
        builder.AddOption("name", ApplicationCommandOptionType.String, "The character name",
            isRequired: true);

        builder.AddOption("class", ApplicationCommandOptionType.String, "The character's class",
            isRequired: true);

        builder.AddOption("level", ApplicationCommandOptionType.Integer, "The character's level",
            minValue: 1, maxValue: 10);

        foreach (var abilityName in AttributeName.AbilityScores)
        {
            builder.AddOption(abilityName.ToLowerInvariant(), ApplicationCommandOptionType.Integer,
                $"The character's {abilityName} score", minValue: 10, maxValue: 20);
        }

        return builder;
    }

    public override async Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var character = Character.CreateNew(command.User.Id);
        if (!TryGetOption<string>(command.Data, "name", out var name) ||
            !AttributeName.TryCanonicalizeMultiPart(name, out var canonicalizedName))
        {
            await command.RespondAsync("Character names must contain only letters and spaces");
            return;
        }

        character.Name = canonicalizedName;

        if (TryGetOption<int>(command.Data, "level", out var level))
        {
            character.Level = level;
        }

        foreach (var abilityName in AttributeName.AbilityScores)
        {
            if (TryGetOption<int>(command.Data, abilityName, out var abilityScore))
            {
                character.AbilityScores[abilityName] = abilityScore;
            }
        }

        // TODO save -- insert with unique index -- must create indexes at this point!
        throw new NotImplementedException();
    }
}
