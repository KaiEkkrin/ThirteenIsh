using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Database;

public static class Extensions
{
    /// <summary>
    /// Adds a tag to this character.
    /// </summary>
    public static bool AddTag(this ITrackedCharacter character, string tagValue)
    {
        ArgumentNullException.ThrowIfNull(character);

        var variables = character.GetVariables();
        if (variables.Tags is not { } tags)
        {
            variables.Tags = [tagValue];
            return true;
        }

        // Keep tags in alphabetical order, and deduplicate (ignoring case)
        if (variables.Tags.Any(tag => tag.Equals(tagValue, StringComparison.OrdinalIgnoreCase))) return false;
        variables.Tags.Add(tagValue);
        variables.Tags.Sort(StringComparer.OrdinalIgnoreCase);
        return true;
    }

    /// <summary>
    /// Finds the first index in a list where the value matches the predicate
    /// I have no idea why there's no official extension for this
    /// </summary>
    public static int FindIndex<T>(this IList<T> list, Func<T, bool> predicate)
    {
        for (var i = 0; i < list.Count; ++i)
        {
            if (predicate(list[i])) return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the index of the value that matches the predicate so long as exactly one value does.
    /// Otherwise, returns -1.
    /// </summary>
    public static int FindUniqueIndex<T>(this IList<T> list, Func<T, bool> predicate)
    {
        var index = -1;
        for (var i = 0; i < list.Count; ++i)
        {
            if (!predicate(list[i])) continue;
            if (index >= 0) return -1;
            index = i;
        }

        return index;
    }

    /// <summary>
    /// Removes a tag from this character.
    /// </summary>
    public static bool RemoveTag(this ITrackedCharacter character, string tagValue)
    {
        ArgumentNullException.ThrowIfNull(character);

        var variables = character.GetVariables();
        if (variables.Tags is not { } tags) return false;

        var index = tags.FindUniqueIndex(tag => tag.Contains(tagValue, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;

        tags.RemoveAt(index);
        return true;
    }

    public static void SetValue<TValue>(this IList<PropertyValue<TValue>> propertyList, string name, TValue value)
    {
        var index = propertyList.FindIndex(p => p.Name == name);
        if (index >= 0)
        {
            propertyList[index].Value = value;
        }
        else
        {
            propertyList.Add(new PropertyValue<TValue> { Name = name, Value = value });
        }
    }

    public static void Sort<TValue>(this IList<TValue> list, IComparer<TValue>? comparer = null)
    {
        comparer ??= Comparer<TValue>.Default;
        switch (list)
        {
            case List<TValue> concreteList:
                concreteList.Sort(comparer);
                return;

            default:
                var items = new List<TValue>(list);
                items.Sort(comparer);
                list.Clear();
                foreach (var item in items) list.Add(item);
                return;
        }
    }

    public static bool TryGetValue<TValue>(this IList<PropertyValue<TValue>> propertyList, string name,
        [MaybeNullWhen(false)] out TValue value)
    {
        var index = propertyList.FindIndex(p => p.Name == name);
        if (index >= 0)
        {
            value = propertyList[index].Value;
            return true;
        }

        value = default;
        return false;
    }
}
