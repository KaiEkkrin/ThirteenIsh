using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;

namespace ThirteenIsh.Results;

public record CombatantResult(
    [NotNull] Adventure Adventure,
    [NotNull] Encounter Encounter,
    [NotNull] CombatantBase Combatant,
    [NotNull] ITrackedCharacter Character);

