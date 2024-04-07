namespace ThirteenIsh.Database.Entities.Messages;

public class EncounterDamageMessage : EncounterMessageBase
{
    public const string TakeFullControlId = "Full";
    public const string TakeHalfControlId = "Half";
    public const string TakeNoneControlId = "None";

    /// <summary>
    /// The character type.
    /// </summary>
    public required CharacterType CharacterType { get; set; }

    /// <summary>
    /// The alias to deal damage to.
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// The amount of damage.
    /// </summary>
    public required int Damage { get; set; }

    /// <summary>
    /// The variable to apply the damage to.
    /// </summary>
    public required string VariableName { get; set; }
}
