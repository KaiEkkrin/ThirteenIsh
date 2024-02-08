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

    public EmbedFieldBuilder BuildEmbedField(CharacterSheet sheet)
    {
        var rows = properties
            .Where(property => !property.IsHidden)
            .Select(property => new[] { property.Name, property.GetDisplayValue(sheet) });

        var table = DiscordUtil.BuildTable(2, rows, 1);

        return new EmbedFieldBuilder()
            .WithName(groupName)
            .WithValue(table);
    }
}
