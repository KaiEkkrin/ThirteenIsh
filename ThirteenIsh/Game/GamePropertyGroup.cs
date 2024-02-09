using Discord;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// Game properties are logically grouped together for structured display.
/// </summary>
internal class GamePropertyGroup(string groupName, ImmutableList<GamePropertyBase> properties)
{
    public string GroupName => groupName;

    public ImmutableList<GamePropertyBase> Properties => properties;

    public EmbedFieldBuilder? BuildEmbedField(CharacterSheet sheet, string[] onlyTheseProperties)
    {
        var rows = properties
            .Where(property => !property.IsHidden &&
                    (onlyTheseProperties.Length == 0 || onlyTheseProperties.Contains(property.Name)))
            .Select(property => new[] { property.Name, property.GetDisplayValue(sheet) })
            .ToList();

        if (rows.Count == 0) return null;
        var table = DiscordUtil.BuildTable(2, rows, 1);

        return new EmbedFieldBuilder()
            .WithName(groupName)
            .WithValue(table);
    }

    public void AddPropertyChoiceOptions(SelectMenuBuilder builder, Func<GamePropertyBase, bool> predicate)
    {
        foreach (var property in properties)
        {
            if (!predicate(property)) continue;
            builder.AddOption(property.Name, property.Name);
        }
    }

    public void AddPropertyGroupChoiceOptions(SelectMenuBuilder builder, Func<GamePropertyBase, bool> predicate)
    {
        if (!properties.Any(predicate)) return;
        builder.AddOption(groupName, groupName);
    }
}
