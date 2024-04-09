namespace ThirteenIsh.Database;

public static class ListExtensions
{
    /// <summary>
    /// Finds the first index in a list where the value matches the predicate
    /// I have no idea why there's no official extension for this
    /// </summary>
    public static int FindIndex<T>(this IList<T> list, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(predicate);

        for (var i = 0; i < list.Count; ++i)
        {
            if (predicate(list[i])) return i;
        }

        return -1;
    }
}
