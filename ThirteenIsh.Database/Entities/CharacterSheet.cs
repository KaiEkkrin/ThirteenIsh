namespace ThirteenIsh.Database.Entities;

/// <summary>
/// Defines an object containing counters, e.g. the sheet or variables.
/// Must be an interface -- EF Core doesn't support inheritance of owned types (JSON)
/// </summary>
public interface ICounterSheet
{
    public IList<PropertyValue<int>> Counters { get; set; }
}

/// <summary>
/// A concrete sheet of counters, used as variables.
/// </summary>
public class VariablesSheet : ICounterSheet
{
    public virtual IList<PropertyValue<int>> Counters { get; set; } = [];
    public virtual IList<string>? Tags { get; set; }
}

/// <summary>
/// Defines a character sheet.
/// </summary>
public class CharacterSheet : ICounterSheet
{
    public virtual IList<PropertyValue<int>> Counters { get; set; } = [];
    public virtual IList<PropertyValue<string>> Properties { get; set; } = [];
    public virtual IList<CustomCounter>? CustomCounters { get; set; }
}

public record PropertyValue<TValue>(string Name, TValue Value);

public record CustomCounter(string Name, int DefaultValue, GameCounterOptions Options);

[Flags]
public enum GameCounterOptions
{
    None = 0,
    CanRoll = 1,
    HasVariable = 2,
    IsHidden = 4
}
