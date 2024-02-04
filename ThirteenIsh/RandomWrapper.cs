namespace ThirteenIsh;

/// <summary>
/// Wrapping for random number generation enabling unit testing with set values
/// </summary>
public interface IRandomWrapper
{
    int NextInteger(int minValue, int maxValue);
}

public sealed class RandomWrapper : IRandomWrapper
{
    public RandomWrapper()
    {
    }

    public int NextInteger(int minValue, int maxValue)
    {
        return Random.Shared.Next(minValue, maxValue);
    }
}
