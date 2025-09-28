using Discord;

namespace ThirteenIsh.Game;

/// <summary>
/// Game properties are logically grouped together for structured display.
/// </summary>
public class GamePropertyGroup<TProperty>(string groupName, ImmutableList<TProperty> properties)
    where TProperty : GamePropertyBase
{
    public string GroupName => groupName;

    public ImmutableList<TProperty> Properties => properties;

    public EmbedFieldBuilder? BuildEmbedField(ITrackedCharacter character,
        IReadOnlyCollection<string>? onlyTheseProperties)
    {
        var rows = properties
            .Where(property => !property.IsHidden &&
                    (onlyTheseProperties is null || onlyTheseProperties.Contains(property.Name)))
            .Select(property => new TableRow(
                new TableCell(property.Name), new TableCell(property.GetDisplayValue(character), true)))
            .ToList();

        if (rows.Count == 0) return null;
        var table = TableHelper.BuildTable(rows);

        return new EmbedFieldBuilder()
            .WithName(groupName)
            .WithValue(table);
    }

    public EmbedFieldBuilder? BuildEmbedField(ICharacterBase character,
        IReadOnlyCollection<string>? onlyTheseProperties)
    {
        var rows = properties
            .Where(property => !property.IsHidden &&
                    (onlyTheseProperties is null || onlyTheseProperties.Contains(property.Name)))
            .Select(property => new TableRow(
                new TableCell(property.Name), new TableCell(property.GetDisplayValue(character), true)))
            .ToList();

        if (rows.Count == 0) return null;
        var table = TableHelper.BuildTable(rows);

        return new EmbedFieldBuilder()
            .WithName(groupName)
            .WithValue(table);
    }

    public void AddPropertyChoiceOptions(SelectMenuBuilder builder, Func<TProperty, bool> predicate)
    {
        foreach (var property in properties)
        {
            if (!predicate(property)) continue;
            builder.AddOption(property.Name, property.Name);
        }
    }

    public void AddPropertyGroupChoiceOptions(SelectMenuBuilder builder, Func<TProperty, bool> predicate)
    {
        if (!properties.Any(predicate)) return;
        builder.AddOption(groupName, groupName);
    }

    public ImmutableList<TProperty> GetProperties(Func<TProperty, bool> predicate)
    {
        return properties.RemoveAll(property => !predicate(property));
    }
}

public class GamePropertyGroup(string groupName, ImmutableList<GamePropertyBase> properties)
    : GamePropertyGroup<GamePropertyBase>(groupName, properties)
{
}

