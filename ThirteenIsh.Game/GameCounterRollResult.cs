namespace ThirteenIsh.Game;

public enum GameCounterRollError
{
    Success,
    NotRollable,
    NoValue
}

public record GameCounterRollResult
{
    /// <summary>
    /// The counter name.
    /// </summary>
    public required string CounterName { get; init; }

    /// <summary>
    /// The error state of this roll. (If this value is not Success, disregard the other properties.)
    /// </summary>
    public required GameCounterRollError Error { get; init; }

    /// <summary>
    /// The number rolled after modifiers.
    /// </summary>
    public int Roll { get; init; }

    /// <summary>
    /// If success is defined, whether or not this roll was successful.
    /// </summary>
    public bool? Success { get; init; }

    /// <summary>
    /// The roll working.
    /// </summary>
    public string Working { get; init; } = string.Empty;

    public string ErrorMessage => Error switch
    {
        GameCounterRollError.NotRollable => $"{CounterName} is not rollable",
        GameCounterRollError.NoValue => $"No value defined for {CounterName}",
        _ => string.Empty
    };
}
