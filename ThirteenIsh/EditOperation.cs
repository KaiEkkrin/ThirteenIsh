namespace ThirteenIsh;

/// <summary>
/// A functor for editing data within a retry loop.
/// </summary>
public abstract class EditOperation<T, TParam, TResult> where TResult : EditResult<T>
{
    public abstract Task<TResult> DoEditAsync(TParam param, CancellationToken cancellationToken);
}

public abstract class SyncEditOperation<T, TParam, TResult> : EditOperation<T, TParam, TResult>
    where TResult : EditResult<T>
{
    public abstract TResult DoEdit(TParam param);

    public sealed override Task<TResult> DoEditAsync(TParam param, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(DoEdit(param));
    }
}
