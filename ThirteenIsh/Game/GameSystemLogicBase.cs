using System.Text;
using ThirteenIsh.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game;

/// <summary>
/// Base class for custom logic for the game system that does known things.
/// </summary>
internal abstract class GameSystemLogicBase
{
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
    /// Provides a one-line summary of the character for character list purposes.
    /// </summary>
    public abstract string GetCharacterSummary(CharacterSheet sheet);

    protected virtual void AddEncounterHeadingRow(List<string[]> data, Encounter encounter)
    {
        data.Add(["Round", $"{encounter.Round}"]);
    }

    private void BuildEncounterHeadingTable(StringBuilder builder, Encounter encounter)
    {
        List<string[]> data = [];
        AddEncounterHeadingRow(data, encounter);
        DiscordUtil.BuildTable(builder, 2, data, 1);
    }

    public void BuildEncounterInitiativeTable(Adventure adventure, StringBuilder builder, Encounter encounter)
    {
        List<IReadOnlyList<string>> data = new(encounter.Combatants.Count);
        var columnCount = 4; // just a starting guess, rows are dynamically created (but must all match)
        for (var i = 0; i < encounter.Combatants.Count; ++i)
        {
            List<string> row = new(columnCount);
            row.Add(i == encounter.TurnIndex ? "-->" : string.Empty);
            BuildEncounterInitiativeTableRow(adventure, encounter.Combatants[i], row);
            row.Add(i == encounter.TurnIndex ? "<--" : string.Empty);
            data.Add(row);

            columnCount = row.Count;
        }

        DiscordUtil.BuildTable(builder, columnCount, data, 1);
    }

    protected virtual void BuildEncounterInitiativeTableRow(Adventure adventure, CombatantBase combatant,
        List<string> row)
    {
        row.Add($"{combatant.Initiative}");
        row.Add(combatant.Name);
    }

    protected static string BuildPointsEncounterTableCell(Adventure adventure, CombatantBase combatant,
        GameCounter counter)
    {
        if (!combatant.TryGetAdventurer(adventure, out var adventurer)) return "???";

        var currentPoints = counter.GetVariableValue(adventurer);
        var maxPoints = counter.GetValue(adventurer.Sheet);

        var currentPointsString = currentPoints.HasValue ? $"{currentPoints.Value}" : "???";
        var maxPointsString = maxPoints.HasValue ? $"{maxPoints.Value}" : "???";
        return $"{counter.Alias} {currentPointsString}/{maxPointsString}";
    }

    protected virtual bool EncounterNextRound(Encounter encounter, IRandomWrapper random)
    {
        return true;
    }
}
