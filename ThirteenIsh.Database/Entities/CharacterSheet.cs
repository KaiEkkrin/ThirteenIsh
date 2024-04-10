using System.Diagnostics.CodeAnalysis;

namespace ThirteenIsh.Database.Entities;

/// <summary>
/// Defines an object containing counters, e.g. the sheet or variables.
/// TODO This is breaking EF Core, which doesn't support inheritance of owned types.
/// I need to break the class hierarchy (have CharacterSheet and EncounterVariables
/// no longer be derived classes) while still sharing code.
/// Try a few things.
/// </summary>
public class CounterSheet
{
    public virtual IList<CounterValue> Counters { get; set; } = [];

    public void Clear()
    {
        Counters.Clear();
    }

    public int? GetCounter(string name)
    {
        return Counters.FirstOrDefault(c => c.Name == name)?.Value;
    }

    public void SetCounter(string name, int value)
    {
        for (var i = 0; i < Counters.Count; ++i)
        {
            if (Counters[i].Name != name) continue;

            Counters[i] = Counters[i] with { Value = value };
            return;
        }

        Counters.Add(new CounterValue(name, value));
    }
}

/// <summary>
/// Defines a character sheet.
/// </summary>
public class CharacterSheet : CounterSheet
{
    public virtual IList<PropertyValue> Properties { get; set; } = [];

    public string? GetProperty(string name)
    {
        return Properties.FirstOrDefault(c => c.Name == name)?.Value;
    }

    public void SetProperty(string name, string value)
    {
        for (var i = 0; i < Properties.Count; ++i)
        {
            if (Properties[i].Name != name) continue;

            Properties[i] = Properties[i] with { Value = value };
            return;
        }

        Properties.Add(new PropertyValue(name, value));
    }
}

public record PropertyValue(string Name, string Value);

public record CounterValue(string Name, int Value);

