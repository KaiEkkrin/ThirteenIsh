using System.Text;
using ThirteenIsh.Entities;

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
    /// Writes an encounter summary table suitable for being part of a pinned message.
    /// </summary>
    public string EncounterTable(Encounter encounter)
    {
        StringBuilder builder = new();
        BuildEncounterHeadingTable(builder, encounter);
        BuildEncounterInitiativeTable(builder, encounter);
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

    public static void BuildEncounterInitiativeTable(StringBuilder builder, Encounter encounter)
    {
        List<string[]> data = new(encounter.Combatants.Count);
        for (var i = 0; i < encounter.Combatants.Count; ++i)
        {
            string[] row =
                [
                    i == encounter.TurnIndex ? "-->" : string.Empty,
                    $"{encounter.Combatants[i].Initiative}",
                    encounter.Combatants[i].Name,
                    i == encounter.TurnIndex ? "<--" : string.Empty,
                ];

            data.Add(row);
        }

        DiscordUtil.BuildTable(builder, 4, data, 1);
    }
}
