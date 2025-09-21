namespace ThirteenIsh.Parsing;

/// <summary>
/// Wrapping for random number generation enabling unit testing with set values
/// </summary>
public interface IRandomWrapper
{
    int NextInteger(int minValue, int maxValue);
}