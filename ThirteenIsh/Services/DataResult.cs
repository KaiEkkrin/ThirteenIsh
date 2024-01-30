namespace ThirteenIsh.Services;

internal record DataResult<TValue>(bool Success, TValue Value);
