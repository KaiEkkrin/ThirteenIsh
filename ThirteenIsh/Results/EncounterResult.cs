using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Results;

public record EncounterResult([NotNull] Adventure Adventure, [NotNull] Encounter Encounter);
