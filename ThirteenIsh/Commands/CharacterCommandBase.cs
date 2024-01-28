using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;

namespace ThirteenIsh.Commands;

internal abstract class CharacterCommandBase : CommandBase
{
    /// <summary>
    /// Maps the (lowercase) argument names for ability scores to the canonical ones.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> AbilityArgumentNameMap = new();

    static CharacterCommandBase()
    {
        foreach (var abilityName in AttributeName.AbilityScores)
        {
            AbilityArgumentNameMap[abilityName] = abilityName.ToLowerInvariant();
        }
    }

    protected CharacterCommandBase(string name, string description) : base(name, description)
    {
    }

    protected static SlashCommandBuilder AddCharacterSlashCommands(SlashCommandBuilder builder)
    {
        builder.AddOption("level", ApplicationCommandOptionType.Integer, "The character's level",
            minValue: 1, maxValue: 10);

        foreach (var (abilityName, argumentName) in AbilityArgumentNameMap)
        {
            builder.AddOption(argumentName, ApplicationCommandOptionType.Integer,
                $"The character's {abilityName} score", minValue: 1, maxValue: 20);
        }

        return builder;
    }

    protected static void ApplyCharacterSheetOptions(SocketSlashCommand command, CharacterSheet sheet)
    {
        if (TryGetOption<int>(command.Data, "level", out var level))
        {
            sheet.Level = level;
        }

        foreach (var (abilityName, argumentName) in AbilityArgumentNameMap)
        {
            if (TryGetOption<int>(command.Data, argumentName, out var abilityScore))
            {
                sheet.AbilityScores[abilityName] = abilityScore;
            }
        }
    }
}
