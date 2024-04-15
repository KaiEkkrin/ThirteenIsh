using ThirteenIsh.Database;

namespace ThirteenIsh;

/// <summary>
/// A functor for editing data within a retry loop.
/// TODO make all of these synchronous, move all of the entity fetches outside of the operation loop?
/// Seems to be the right approach...
/// </summary>
public abstract class EditOperation<T, TParam, TResult> where TResult : EditResult<T>
{
    /// <summary>
    /// Creates a result representing an error.
    /// Override this particularly when using the EditCharacterAsync method
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>The error result.</returns>
    public virtual EditResult<T> CreateError(string message) => new(default);

    /// <summary>
    /// Does the operation.
    /// </summary>
    public abstract Task<TResult> DoEditAsync(DataContext context, TParam param, CancellationToken cancellationToken);
}

public abstract class SyncEditOperation<T, TParam, TResult> : EditOperation<T, TParam, TResult>
    where TResult : EditResult<T>
{
    public abstract TResult DoEdit(DataContext context, TParam param);

    public sealed override Task<TResult> DoEditAsync(DataContext context, TParam param, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(DoEdit(context, param));
    }
}
