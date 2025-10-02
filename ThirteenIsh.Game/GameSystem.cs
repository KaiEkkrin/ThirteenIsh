using Discord;
using System.Collections.Frozen;
using ThirteenIsh.Game.Swn;

namespace ThirteenIsh.Game;

/// <summary>
/// Describes a game system, providing game-specific ways of interacting with it.
/// </summary>
public abstract class GameSystem
{
    private readonly FrozenDictionary<string, CharacterSystem> _characterSystemsByName;
    private readonly FrozenDictionary<CharacterType, CharacterSystem> _defaultCharacterSystems;

    protected GameSystem(string name, IEnumerable<CharacterSystem> characterSystems)
    {
        Name = name;
        var systemsList = characterSystems.ToList();
        ValidateCharacterSystems(systemsList);

        _characterSystemsByName = systemsList.ToFrozenDictionary(cs => cs.Name);
        _defaultCharacterSystems = systemsList
            .Where(cs => cs.DefaultForType.HasValue)
            .ToFrozenDictionary(cs => cs.DefaultForType!.Value);
    }

    public static readonly IReadOnlyList<GameSystem> AllGameSystems =
    [
        ThirteenthAge.ThirteenthAgeSystem.Build(),
        // TODO : Dragonbane game system is disabled until we make a monster
        // implementation that works with it.
        // Dragonbane.DragonbaneSystem.Build(),
        SwnSystem.Build()
    ];

    /// <summary>
    /// The Unset label shown to the user when character properties haven't been set yet.
    /// </summary>
    public const string Unset = "(unset)";

    /// <summary>
    /// This game system's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Writes an encounter summary table suitable for being part of a pinned message.
    /// </summary>
    public async Task<string> BuildEncounterTableAsync(ICharacterDataService characterDataService,
        Encounter encounter, CancellationToken cancellationToken = default)
    {
        StringBuilder builder = new();
        BuildEncounterHeadingTable(builder, encounter);
        await BuildEncounterInitiativeTableAsync(characterDataService, builder, encounter, cancellationToken);
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

    public static SlashCommandOptionBuilder BuildGameSystemAndCharacterSystemChoiceOption(string name, CharacterType characterType)
    {
        var builder = new SlashCommandOptionBuilder()
            .WithName(name)
            .WithDescription("The game system.")
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.String);

        var compatibilityFlag = GetCompatibilityFlag(characterType);

        foreach (var gameSystem in AllGameSystems)
        {
            var compatibleCharacterSystems = gameSystem.GetCharacterSystems()
                .Where(cs => cs.Compatibility.HasFlag(compatibilityFlag))
                .ToList();

            if (compatibleCharacterSystems.Count == 0)
                continue;

            if (compatibleCharacterSystems.Count == 1)
            {
                // Single character system - just show game system name
                var value = $"{gameSystem.Name}";
                builder.AddChoice(gameSystem.Name, value);
            }
            else
            {
                // Multiple character systems - show "Game System -- Character System"
                foreach (var characterSystem in compatibleCharacterSystems)
                {
                    var label = $"{gameSystem.Name} -- {characterSystem.Name}";
                    var value = $"{gameSystem.Name}::{characterSystem.Name}";
                    builder.AddChoice(label, value);
                }
            }
        }

        return builder;
    }

    public static bool TryParseGameSystemAndCharacterSystemChoice(
        string selection,
        CharacterType characterType,
        [MaybeNullWhen(false)] out GameSystem gameSystem,
        [MaybeNullWhen(false)] out CharacterSystem characterSystem,
        [MaybeNullWhen(true)] out string errorMessage)
    {
        var parts = selection.Split("::", StringSplitOptions.None);
        var gameSystemName = parts[0];

        gameSystem = AllGameSystems.FirstOrDefault(gs => gs.Name == gameSystemName);
        if (gameSystem is null)
        {
            characterSystem = null;
            errorMessage = $"Game system '{gameSystemName}' not found.";
            return false;
        }

        string? characterSystemName = parts.Length > 1 ? parts[1] : null;

        try
        {
            characterSystem = gameSystem.GetCharacterSystem(characterType, characterSystemName);
            errorMessage = null;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            characterSystem = null;
            errorMessage = ex.Message;
            return false;
        }
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
    public abstract string GetCharacterSummary(ICharacterBase character);

    /// <summary>
    /// Gets the character system for a character type and optional system name.
    /// </summary>
    public CharacterSystem GetCharacterSystem(CharacterType characterType, string? characterSystemName)
    {
        if (string.IsNullOrEmpty(characterSystemName))
        {
            if (_defaultCharacterSystems.TryGetValue(characterType, out var defaultSystem))
                return defaultSystem;
            throw new InvalidOperationException($"No default character system found for {characterType} in {Name}");
        }

        if (_characterSystemsByName.TryGetValue(characterSystemName, out var namedSystem))
        {
            if (namedSystem.Compatibility.HasFlag(GetCompatibilityFlag(characterType)))
                return namedSystem;
            throw new InvalidOperationException($"Character system '{characterSystemName}' does not support {characterType} in {Name}");
        }

        throw new InvalidOperationException($"Character system '{characterSystemName}' not found in {Name}");
    }

    /// <summary>
    /// Gets all character systems for this game system.
    /// </summary>
    public IEnumerable<CharacterSystem> GetCharacterSystems()
    {
        return _characterSystemsByName.Values;
    }

    private static CharacterTypeCompatibility GetCompatibilityFlag(CharacterType characterType)
    {
        return characterType switch
        {
            CharacterType.PlayerCharacter => CharacterTypeCompatibility.PlayerCharacter,
            CharacterType.Monster => CharacterTypeCompatibility.Monster,
            _ => throw new ArgumentException($"Unknown character type: {characterType}")
        };
    }

    private static void ValidateCharacterSystems(IList<CharacterSystem> characterSystems)
    {
        // Check that each CharacterSystem supports at least one CharacterType
        foreach (var cs in characterSystems)
        {
            if (cs.Compatibility == CharacterTypeCompatibility.None)
                throw new InvalidOperationException($"Character system '{cs.Name}' must support at least one character type");
        }

        // Check that each CharacterSystem with DefaultForType actually supports that type
        foreach (var cs in characterSystems.Where(cs => cs.DefaultForType.HasValue))
        {
            var requiredFlag = GetCompatibilityFlag(cs.DefaultForType!.Value);
            if (!cs.Compatibility.HasFlag(requiredFlag))
                throw new InvalidOperationException($"Character system '{cs.Name}' is marked as default for {cs.DefaultForType} but does not support that character type");
        }

        // Check that each CharacterType has exactly one default
        var characterTypes = Enum.GetValues<CharacterType>();
        foreach (var characterType in characterTypes)
        {
            var defaultsForType = characterSystems.Count(cs => cs.DefaultForType == characterType);
            if (defaultsForType == 0)
                throw new InvalidOperationException($"No default character system found for {characterType}");
            if (defaultsForType > 1)
                throw new InvalidOperationException($"Multiple default character systems found for {characterType}");
        }

        // Check for duplicate names
        var duplicateNames = characterSystems.GroupBy(cs => cs.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateNames.Count > 0)
            throw new InvalidOperationException($"Duplicate character system names found: {string.Join(", ", duplicateNames)}");
    }

    protected virtual void AddEncounterHeadingRow(List<TableRow> data, Encounter encounter)
    {
        data.Add(new TableRow(new TableCell("Round"), TableCell.FromNumber(encounter.Round)));
    }

    private void BuildEncounterHeadingTable(StringBuilder builder, Encounter encounter)
    {
        List<TableRow> data = [];
        AddEncounterHeadingRow(data, encounter);
        TableHelper.BuildTableEx(builder, data, false, maxTableWidth: TableHelper.MaxPinnedTableWidth);
    }

    public async Task BuildEncounterInitiativeTableAsync(
        ICharacterDataService characterDataService,
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
            var characterSystem = GetCharacterSystem(combatant.CharacterType, null);
            var character = await characterDataService.GetCharacterAsync(combatant, encounter, cancellationToken)
                ?? throw new GamePropertyException($"Character not found for {combatant.Alias}");

            StringBuilder combatantAliasBuilder = new(combatant.Alias.Length + 5);
            if (combatant.Alias == encounter.TurnAlias) combatantAliasBuilder.Append('+');
            combatantAliasBuilder.Append(combatant.Alias);
            characterSystem.DecorateCharacterAlias(combatantAliasBuilder, character);

            List<TableCell> cells = [
                new TableCell(combatantAliasBuilder.ToString()),
                TableCell.FromNumber(combatant.Initiative)
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
