using Discord;
using System.Collections.Frozen;
using System.Text;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// Describes a game system, providing game-specific ways of interacting with it.
/// </summary>
internal abstract class GameSystem(string name, IEnumerable<CharacterSystem> characterSystems)
{
    private readonly FrozenDictionary<CharacterType, CharacterSystem> _characterSystems =
        characterSystems.ToFrozenDictionary(o => o.CharacterType);

    public static readonly IReadOnlyList<GameSystem> AllGameSystems =
    [
        ThirteenthAge.ThirteenthAgeSystem.Build(),
        Dragonbane.DragonbaneSystem.Build()
    ];

    /// <summary>
    /// The "Custom" category.
    /// </summary>
    public const string Custom = "Custom";

    /// <summary>
    /// The Unset label shown to the user when character properties haven't been set yet.
    /// </summary>
    public const string Unset = "(unset)";

    /// <summary>
    /// This game system's name.
    /// </summary>
    public string Name { get; } = name;

    public static SlashCommandOptionBuilder BuildGameSystemChoiceOption(string name)
    {
        var builder = new SlashCommandOptionBuilder()
            .WithName(name)
            .WithDescription("The game system.")
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.String);

        foreach (var gameSystem in AllGameSystems)
        {
            builder.AddChoice(gameSystem.Name, gameSystem.Name);
        }

        return builder;
    }

    /// <summary>
    /// Sets up the beginning of an encounter.
    /// </summary>
    public abstract void EncounterBegin(Encounter encounter);

    /// <summary>
    /// Has this adventurer join an encounter. Returns the roll result and emits
    /// the working.
    /// If this adventurer cannot join the encounter, returns null.
    /// </summary>
    public abstract GameCounterRollResult? EncounterJoin(
        Adventurer adventurer,
        Encounter encounter,
        NameAliasCollection nameAliasCollection,
        IRandomWrapper random,
        int rerolls,
        ulong userId);

    /// <summary>
    /// Moves to the next combatant in the encounter. Returns the new turn index or null if
    /// the encounter could not be progressed.
    /// </summary>
    public int? EncounterNext(Encounter encounter, IRandomWrapper random)
    {
        if (encounter.Combatants.Count == 0) return null;

        if (encounter.TurnIndex.HasValue) encounter.TurnIndex += 1;
        else encounter.TurnIndex = 0;

        if (encounter.TurnIndex >= encounter.Combatants.Count)
        {
            encounter.TurnIndex = 0;
            ++encounter.Round;
            return EncounterNextRound(encounter, random) ? encounter.TurnIndex : null;
        }

        return encounter.TurnIndex;
    }

    /// <summary>
    /// Writes an encounter summary table suitable for being part of a pinned message.
    /// </summary>
    public string EncounterTable(Adventure adventure, Encounter encounter)
    {
        StringBuilder builder = new();
        BuildEncounterHeadingTable(builder, encounter);
        BuildEncounterInitiativeTable(adventure, builder, encounter);
        return builder.ToString();
    }

    /// <summary>
    /// Gets the named game system.
    /// </summary>
    public static GameSystem Get(string name) => AllGameSystems.First(o => o.Name == name);

    /// <summary>
    /// Provides a one-line summary of the character for character list purposes.
    /// </summary>
    public abstract string GetCharacterSummary(CharacterSheet sheet, CharacterType type);

    /// <summary>
    /// Gets the character system for a character type.
    /// </summary>
    public CharacterSystem GetCharacterSystem(CharacterType characterType) => _characterSystems[characterType];

    protected virtual void AddEncounterHeadingRow(List<TableRowBase> data, Encounter encounter)
    {
        data.Add(new TableRow(new TableCell("Round"), TableCell.Integer(encounter.Round)));
    }

    private void BuildEncounterHeadingTable(StringBuilder builder, Encounter encounter)
    {
        List<TableRowBase> data = [];
        AddEncounterHeadingRow(data, encounter);
        TableHelper.BuildTableEx(builder, 2, data, false);
    }

    public void BuildEncounterInitiativeTable(Adventure adventure, StringBuilder stringBuilder, Encounter encounter)
    {
        EncounterInitiativeTableBuilder tableBuilder = new();
        for (var i = 0; i < encounter.Combatants.Count; ++i)
        {
            // Leave a blank row between each combatant so that the table is readable
            if (i > 0) tableBuilder.AddRow(TableCell.Empty, TableCell.Empty);

            var combatant = encounter.Combatants[i];
            tableBuilder.AddRow(
                TableCell.Integer(combatant.Initiative),
                new TableCell(combatant.Alias),
                i == encounter.TurnIndex);

            tableBuilder.AddSpanningRow(combatant.Name, true);

            BuildEncounterInitiativeTableRows(adventure, combatant, tableBuilder);
        }

        TableHelper.BuildTableEx(stringBuilder, 3, tableBuilder.Data, false);
    }

    protected abstract void BuildEncounterInitiativeTableRows(Adventure adventure, CombatantBase combatant,
        EncounterInitiativeTableBuilder builder);

    protected static string BuildPointsEncounterTableCell(Adventure adventure, CombatantBase combatant,
        GameCounter counter)
    {
        if (!combatant.TryGetAdventurer(adventure, out var adventurer)) return "???";

        var currentPoints = counter.GetVariableValue(adventurer);
        var maxPoints = counter.GetValue(adventurer.Sheet);

        var currentPointsString = currentPoints.HasValue ? $"{currentPoints.Value}" : "???";
        var maxPointsString = maxPoints.HasValue ? $"{maxPoints.Value}" : "???";
        return $"{currentPointsString}/{maxPointsString}";
    }

    protected virtual bool EncounterNextRound(Encounter encounter, IRandomWrapper random)
    {
        return true;
    }

    public readonly struct DamageCounter
    {
        public GameCounter Counter { get; init; }
        public int Multiplier { get; init; }
    }

    protected sealed class EncounterInitiativeTableBuilder
    {
        private readonly List<TableRowBase> _data = [];

        public IReadOnlyList<TableRowBase> Data => _data;

        public void AddRow(TableCell label, TableCell value, bool withPointyBit = false)
        {
            _data.Add(new TableRow(
                new TableCell(withPointyBit ? "-->" : "   "),
                label,
                value));
        }

        public void AddSpanningRow(string text, bool rightJustify)
        {
            _data.Add(new SpanningTableRow(text, rightJustify, ' '));
        }
    }
}

