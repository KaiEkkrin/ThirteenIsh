using Shouldly;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Tests.Mocks;

/// <summary>
/// Expects to produce a sequence of rolls of (dice size, result), followed
/// by no more calls.
/// </summary>
internal sealed class MockRandomWrapper(params int[] expectations) : IRandomWrapper
{
    private int _index;

    public void AssertCompleted() => _index.ShouldBe(expectations.Length);

    public int NextInteger(int minValue, int maxValue)
    {
        _index.ShouldBeLessThan(expectations.Length); // otherwise we've overrun :)

        minValue.ShouldBe(1); // not supporting anything else right now
        maxValue.ShouldBe(expectations[_index] + 1); // `maxValue` is exclusive

        var result = expectations[_index + 1];
        _index += 2;
        return result;
    }
}
