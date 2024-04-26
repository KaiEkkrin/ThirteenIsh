using ThirteenIsh.Database;

namespace ThirteenIsh;

/// <summary>
/// A functor for editing data within a retry loop.
/// TODO remove MessageEditResult and make the base EditResult always carry a message instead.
/// Make CreateError sensible (right now it's always returning nulls.)
/// </summary>
public abstract class EditOperation<T, TParam> where T : class
{
    /// <summary>
    /// Creates a result representing an error.
    /// Override this particularly when using the EditCharacterAsync method
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>The error result.</returns>
    public virtual EditResult<T> CreateError(string message) => new(default, message);

    /// <summary>
    /// Does the operation.
    /// </summary>
    public abstract Task<EditResult<T>> DoEditAsync(DataContext context, TParam param, CancellationToken cancellationToken);
}

public abstract class SyncEditOperation<T, TParam> : EditOperation<T, TParam> where T : class
{
    public abstract EditResult<T> DoEdit(DataContext context, TParam param);

    public sealed override Task<EditResult<T>> DoEditAsync(DataContext context, TParam param,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(DoEdit(context, param));
    }
}
