using Discord;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// Game properties are logically grouped together for structured display.
/// </summary>
internal class GamePropertyGroup<TProperty>(string groupName, ImmutableList<TProperty> properties)
    where TProperty : GamePropertyBase
{
    public string GroupName => groupName;

    public ImmutableList<TProperty> Properties => properties;

    public EmbedFieldBuilder? BuildEmbedField(Adventurer adventurer, IReadOnlyCollection<string>? onlyTheseProperties)
    {
        var rows = properties
            .Where(property => !property.IsHidden &&
                    (onlyTheseProperties is not { Count: > 0 } || onlyTheseProperties.Contains(property.Name)))
            .Select(property => new[] { property.Name, property.GetDisplayValue(adventurer) })
            .ToList();

        if (rows.Count == 0) return null;
        var table = DiscordUtil.BuildTable(2, rows, 1);

        return new EmbedFieldBuilder()
            .WithName(groupName)
            .WithValue(table);
    }

    public EmbedFieldBuilder? BuildEmbedField(CharacterSheet sheet, IReadOnlyCollection<string>? onlyTheseProperties)
    {
        var rows = properties
            .Where(property => !property.IsHidden &&
                    (onlyTheseProperties is not { Count: > 0 } || onlyTheseProperties.Contains(property.Name)))
            .Select(property => new[] { property.Name, property.GetDisplayValue(sheet) })
            .ToList();

        if (rows.Count == 0) return null;
        var table = DiscordUtil.BuildTable(2, rows, 1);

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

internal class GamePropertyGroup(string groupName, ImmutableList<GamePropertyBase> properties)
    : GamePropertyGroup<GamePropertyBase>(groupName, properties)
{
}

