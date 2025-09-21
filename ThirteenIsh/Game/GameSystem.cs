using Discord;
using System.Collections.Frozen;
using System.Text;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game.Swn;
using ThirteenIsh.Parsing;
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
        Dragonbane.DragonbaneSystem.Build(),
        SwnSystem.Build()
    ];

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
    public async Task<string> BuildEncounterTableAsync(SqlDataService dataService,
        Encounter encounter, CancellationToken cancellationToken = default)
    {
        StringBuilder builder = new();
        BuildEncounterHeadingTable(builder, encounter);
        await BuildEncounterInitiativeTableAsync(dataService, builder, encounter, cancellationToken);
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
    /// If this monster cannot join the encounter, returns an error roll result.
    /// </summary>
    public abstract EncounterRollResult EncounterAdd(
        Character character,
        Encounter encounter,
        NameAliasCollection nameAliasCollection,
        IRandomWrapper random,
        int rerolls,
        int swarmCount,
        ulong userId);

    /// <summary>
    /// Sets up the beginning of an encounter.
    /// </summary>
    public abstract void EncounterBegin(Encounter encounter);

    /// <summary>
    /// Has this adventurer join an encounter. Returns the roll result and emits
    /// the working.
    /// If this adventurer cannot join the encounter, returns an error roll result.
    /// </summary>
    public abstract EncounterRollResult EncounterJoin(
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
        if (!newRound) return nextCombatant;

        ++encounter.Round;
        return EncounterNextRound(encounter, random);
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
    /// Provides a one-line summary of the character for character list purposes.
    /// </summary>
    public virtual string GetCharacterSummary(ITrackedCharacter character)
    {
        return GetCharacterSummary(character.Sheet, character.Type);
    }

    /// <summary>
    /// Gets the character system for a character type.
    /// </summary>
    public CharacterSystem GetCharacterSystem(CharacterType characterType) => _characterSystems[characterType];

    protected virtual void AddEncounterHeadingRow(List<TableRow> data, Encounter encounter)
    {
        data.Add(new TableRow(new TableCell("Round"), TableCell.Integer(encounter.Round)));
    }

    private void BuildEncounterHeadingTable(StringBuilder builder, Encounter encounter)
    {
        List<TableRow> data = [];
        AddEncounterHeadingRow(data, encounter);
        TableHelper.BuildTableEx(builder, data, false, maxTableWidth: TableHelper.MaxPinnedTableWidth);
    }

    public async Task BuildEncounterInitiativeTableAsync(
        SqlDataService dataService,
        StringBuilder stringBuilder,
        Encounter encounter,
        CancellationToken cancellationToken = default)
    {
        if (!encounter.Combatants.Any()) return;

        // The encounter may contain mixed character types, and I need to ensure every table row has the
        // same number of cells, so I need to work out the cells for each character in advance and pad them
        // all to the longest one:
        List<List<TableCell>> rowPrototypes = [];
        var maxCellCount = int.MinValue;
        foreach (var combatant in encounter.CombatantsInTurnOrder)
        {
            var characterSystem = GetCharacterSystem(combatant.CharacterType);
            var character = await dataService.GetCharacterAsync(combatant, encounter, cancellationToken)
                ?? throw new InvalidOperationException($"Character not found for {combatant.Alias}");

            StringBuilder combatantAliasBuilder = new(combatant.Alias.Length + 5);
            if (combatant.Alias == encounter.TurnAlias) combatantAliasBuilder.Append('+');
            combatantAliasBuilder.Append(combatant.Alias);
            characterSystem.DecorateCharacterAlias(combatantAliasBuilder, character);

            List<TableCell> cells = [
                new TableCell(combatantAliasBuilder.ToString()),
                TableCell.Integer(combatant.Initiative)
                ];

            foreach (var counter in characterSystem.GetEncounterTableCounters(character.Sheet))
            {
                cells.Add(new TableCell($"{counter.Alias} {counter.GetDisplayValue(character)}"));
            }

            if (character.GetVariables().Tags is { Count: > 0 } tags)
            {
                foreach (var tag in tags) cells.Add(new TableCell(tag));
            }

            rowPrototypes.Add(cells);
            maxCellCount = Math.Max(maxCellCount, cells.Count);
        }

        // Now I can build the final table
        List<TableRow> rows = new(rowPrototypes.Count);
        foreach (var rowPrototype in rowPrototypes)
        {
            var array = new TableCell[maxCellCount];
            rowPrototype.CopyTo(array, 0);
            for (var i = rowPrototype.Count; i < maxCellCount; ++i) array[i] = TableCell.Empty;

            rows.Add(new TableRow(array));
        }

        TableHelper.BuildTableEx(stringBuilder, rows, false, maxTableWidth: TableHelper.MaxPinnedTableWidth,
            language: "diff");
    }

    protected abstract CombatantBase? EncounterNextRound(Encounter encounter, IRandomWrapper random);

    public readonly struct DamageCounter
    {
        public GameCounter Counter { get; init; }
        public int Multiplier { get; init; }
    }
}
