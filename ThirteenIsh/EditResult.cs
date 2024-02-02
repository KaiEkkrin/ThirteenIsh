namespace ThirteenIsh;

/// <summary>
/// The result of making custom changes to an entity via an action.
/// </summary>
/// <typeparam name="T">The value type to return to the caller.</typeparam>
/// <param name="value">The value to return to the caller.</param>
public class EditResult<T>(T? value)
{
    /// <summary>
    /// If true, the entity should be written back to the database; otherwise it should not.
    /// </summary>
    public virtual bool Success => value != null;

    /// <summary>
    /// The result value.
    /// </summary>
    public T? Value => value;
}

/// <summary>
/// An extension to EditResult that allows for a custom error message to be returned.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="value"></param>
/// <param name="errorMessage"></param>
public class MessageEditResult<T>(T? value, string? errorMessage = null)
    : EditResult<ResultOrMessage<T>>(new ResultOrMessage<T>(value, errorMessage))
{
    public override bool Success => string.IsNullOrEmpty(errorMessage);
}

/// <summary>
/// Carries around either a result or an error message.
/// </summary>
public readonly record struct ResultOrMessage<T>(T? Value, string? ErrorMessage = null);

