namespace ThirteenIsh.Game;

public record EncounterRollResult : GameCounterRollResult
{
    public string Alias { get; init; } = string.Empty;

    public static EncounterRollResult BuildError(GameCounterRollResult result)
    {
        if (result.Error == GameCounterRollError.Success)
            throw new ArgumentException("Error result expected");

        return new EncounterRollResult
        {
            CounterName = result.CounterName,
            Error = result.Error,
            Roll = result.Roll,
            Success = result.Success,
            Working = result.Working
        };
    }

    public static EncounterRollResult BuildSuccess(GameCounterRollResult result, string alias)
    {
        if (result.Error != GameCounterRollError.Success) throw new ArgumentException("Success result expected");
        if (string.IsNullOrEmpty(alias)) throw new ArgumentException("An alias is required");

        return new EncounterRollResult
        {
            Alias = alias,
            CounterName = result.CounterName,
            Error = GameCounterRollError.Success,
            Roll = result.Roll,
            Success = result.Success,
            Working = result.Working
        };
    }
}
