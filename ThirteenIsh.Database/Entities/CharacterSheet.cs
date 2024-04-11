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
/// A concrete sheet of counters.
/// </summary>
public class CounterSheet : ICounterSheet
{
    public IList<PropertyValue<int>> Counters { get; set; } = [];
}

/// <summary>
/// Defines a character sheet.
/// </summary>
public class CharacterSheet : ICounterSheet
{
    public virtual IList<PropertyValue<int>> Counters { get; set; } = [];
    public virtual IList<PropertyValue<string>> Properties { get; set; } = [];
}

public record PropertyValue<TValue>(string Name, TValue Value);

