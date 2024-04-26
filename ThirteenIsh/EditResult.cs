namespace ThirteenIsh;

/// <summary>
/// The result of making custom changes to an entity via an action.
/// </summary>
/// <typeparam name="T">The value type to return to the caller.</typeparam>
/// <param name="value">The value to return to the caller.</param>
/// <param name="errorMessage">If set, an error message.</param>
public class EditResult<T>(T? value, string? errorMessage = null) where T : class
{
    public bool Success => string.IsNullOrEmpty(errorMessage) && (value != null ? true
        : throw new InvalidOperationException("Entirely null EditResult found"));

    public TOutput Handle<TOutput>(Func<string, TOutput> onError, Func<T, TOutput> onValue)
    {
        if (!string.IsNullOrEmpty(errorMessage))
        {
            return onError(errorMessage);
        }
        else if (value != null)
        {
            return onValue(value);
        }
        else
        {
            throw new InvalidOperationException("Entirely null EditResult found");
        }
    }

    public async Task<TOutput> HandleAsync<TOutput>(Func<string, TOutput> onError, Func<T, Task<TOutput>> onValueAsync)
    {
        if (!string.IsNullOrEmpty(errorMessage))
        {
            return onError(errorMessage);
        }
        else if (value != null)
        {
            return await onValueAsync(value);
        }
        else
        {
            throw new InvalidOperationException("Entirely null EditResult found");
        }
    }
}

