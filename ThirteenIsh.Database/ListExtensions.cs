using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Database;

public static class ListExtensions
{
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

    public static void SetValue<TValue>(this IList<PropertyValue<TValue>> propertyList, string name, TValue value)
    {
        var index = propertyList.FindIndex(p => p.Name == name);
        if (index >= 0)
        {
            propertyList[index] = propertyList[index] with { Value = value };
        }
        else
        {
            propertyList.Add(new PropertyValue<TValue>(name, value));
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
