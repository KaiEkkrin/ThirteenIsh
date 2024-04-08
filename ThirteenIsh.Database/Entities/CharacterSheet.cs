namespace ThirteenIsh.Database.Entities;

/// <summary>
/// Defines an object containing counters, e.g. the sheet or variables.
/// </summary>
public class CounterValueSet
{
    public List<CounterValue> Counters { get; set; } = [];

    public int? GetCounter(string name)
    {
        return Counters.FirstOrDefault(c => c.Name == name)?.Value;
    }

    public void SetCounter(string name, int value)
    {
        var index = Counters.FindIndex(c => c.Name == name);
        if (index >= 0)
        {
            Counters[index] = Counters[index] with { Value = value };
        }
        else
        {
            Counters.Add(new CounterValue(name, value));
        }
    }
}

/// <summary>
/// Defines a character sheet.
/// 
/// </summary>
public class CharacterSheet : CounterValueSet
{
    public List<PropertyValue> Properties { get; set; } = [];

    public string? GetProperty(string name)
    {
        return Properties.FirstOrDefault(c => c.Name == name)?.Value;
    }

    public void SetProperty(string name, string value)
    {
        var index = Properties.FindIndex(c => c.Name == name);
        if (index >= 0)
        {
            Properties[index] = Properties[index] with { Value = value };
        }
        else
        {
            Properties.Add(new PropertyValue(name, value));
        }
    }
}

public record PropertyValue(string Name, string Value);

public record CounterValue(string Name, int Value);

