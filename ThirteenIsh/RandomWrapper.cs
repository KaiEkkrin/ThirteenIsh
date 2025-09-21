using System.Security.Cryptography;
using ThirteenIsh.Parsing;

namespace ThirteenIsh;

public sealed class RandomWrapper : IRandomWrapper
{
    public RandomWrapper()
    {
    }

    public int NextInteger(int minValue, int maxValue)
    {
        return RandomNumberGenerator.GetInt32(minValue, maxValue);
    }
}
