using Discord;
using MongoDB.Driver;
using System.Collections.Frozen;
using System.Text;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Services;

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

    /// <summary>
    /// Writes an encounter summary table suitable for being part of a pinned message.
    /// </summary>
    public async Task<string> BuildEncounterTableAsync(SqlDataService dataService, Adventure adventure,
        Encounter encounter, CancellationToken cancellationToken = default)
    {
        StringBuilder builder = new();
        BuildEncounterHeadingTable(builder, encounter);
        await BuildEncounterInitiativeTableAsync(dataService, adventure, builder, encounter, cancellationToken);
        return builder.ToString();
    }

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
    /// Adds a monster to the encounter. Returns the roll result and emits the working;
    /// also populates the string out parameter with the new alias for the monster.
    /// If this monster cannot join the encounter, returns null.
    /// </summary>
    public abstract GameCounterRollResult? EncounterAdd(
        DataContext dataContext,
        Character character,
        Encounter encounter,
        NameAliasCollection nameAliasCollection,
        IRandomWrapper random,
        int rerolls,
        ulong userId,
        out string alias);

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
        DataContext dataContext,
        Adventurer adventurer,
        Encounter encounter,
        NameAliasCollection nameAliasCollection,
        IRandomWrapper random,
        int rerolls,
        ulong userId);

    /// <summary>
    /// Moves to the next combatant in the encounter. Returns the next combatant, or null if
    /// the encounter could not be progressed.
    /// </summary>
    public CombatantBase? EncounterNext(Encounter encounter, IRandomWrapper random)
    {
        if (!encounter.Combatants.Any()) return null;

        var nextCombatant = encounter.NextTurn(out var newRound);
        return newRound ? EncounterNextRound(encounter, random) : nextCombatant;
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
        TableHelper.BuildTableEx(builder, 2, data, false, maxTableWidth: TableHelper.MaxPinnedTableWidth);
    }

    public async Task BuildEncounterInitiativeTableAsync(
        SqlDataService dataService,
        Adventure adventure,
        StringBuilder stringBuilder,
        Encounter encounter,
        CancellationToken cancellationToken = default)
    {
        EncounterInitiativeTableBuilder tableBuilder = new();

        // trust the encounter to have the up-to-date combatants list
        var leaveBlankRow = false;
        foreach (var combatant in encounter.CombatantsInTurnOrder)
        {
            // Leave a blank row between each combatant so that the table is readable?
            if (leaveBlankRow) tableBuilder.AddRow(TableCell.Empty, TableCell.Empty);

            tableBuilder.AddingActiveRows = combatant.Alias == encounter.TurnAlias;
            tableBuilder.AddRow(
                TableCell.Integer(combatant.Initiative),
                new TableCell(combatant.Alias));

            tableBuilder.AddSpanningRow(combatant.Name);

            await BuildEncounterInitiativeTableRowsAsync(dataService, adventure, combatant, tableBuilder,
                cancellationToken);

            leaveBlankRow = true;
        }

        TableHelper.BuildTableEx(stringBuilder, 2, tableBuilder.Data, false, '\u00a0',
            maxTableWidth: TableHelper.MaxPinnedTableWidth, language: "diff");
    }

    protected abstract Task BuildEncounterInitiativeTableRowsAsync(
        SqlDataService dataService,
        Adventure adventure,
        CombatantBase combatant,
        EncounterInitiativeTableBuilder builder,
        CancellationToken cancellationToken = default);

    protected static async Task<string> BuildPointsEncounterTableCellAsync(
        SqlDataService dataService,
        CombatantBase combatant,
        GameCounter counter,
        CancellationToken cancellationToken = default)
    {
        var character = await dataService.GetCharacterAsync(combatant, cancellationToken);
        if (character is null) return "???";

        var currentPoints = counter.GetVariableValue(character);
        var maxPoints = counter.GetValue(character.Sheet);

        var currentPointsString = currentPoints.HasValue ? $"{currentPoints.Value}" : "???";
        var maxPointsString = maxPoints.HasValue ? $"{maxPoints.Value}" : "???";
        return $"{currentPointsString}/{maxPointsString}";
    }

    protected abstract CombatantBase? EncounterNextRound(Encounter encounter, IRandomWrapper random);

    public readonly struct DamageCounter
    {
        public GameCounter Counter { get; init; }
        public int Multiplier { get; init; }
    }

    protected sealed class EncounterInitiativeTableBuilder
    {
        private readonly List<TableRowBase> _data = [];

        public bool AddingActiveRows { get; set; }

        private char ActiveCharacter => AddingActiveRows ? '+' : ' ';

        public IReadOnlyList<TableRowBase> Data => _data;

        public void AddRow(TableCell label, TableCell value)
        {
            _data.Add(new TableRow(
                label with { Text = $"{ActiveCharacter}{label.Text}" },
                value));
        }

        public void AddSpanningRow(string text)
        {
            _data.Add(new SpanningTableRow($"{ActiveCharacter}{text}"));
        }
    }
}

