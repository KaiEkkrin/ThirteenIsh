using Shouldly;
using ThirteenIsh.Parsing;
using ThirteenIsh.Tests.Mocks;

namespace ThirteenIsh.Tests;

public class ParsingTests
{
    public static readonly TheoryData<string, int> AboveMaxDepthExpressions = new()
    {
        { string.Join(" + ", Enumerable.Repeat("1", ParserBase.MaxDepth / 4)), ParserBase.MaxDepth / 4 }
    };

    public static readonly TheoryData<string> BelowMaxDepthExpressions = new()
    {
        { string.Join(" + ", Enumerable.Repeat("1", ParserBase.MaxDepth)) }
    };

    [Theory]
    [InlineData("11", 11)]
    [InlineData("-11", -11)]
    [InlineData("3+4+5", 12)]
    [InlineData("13-4-5", 4)]
    [InlineData("13-4--5", 14)]
    [InlineData("13--4-5", 12)]
    [InlineData("13--4--5", 22)]
    [InlineData("-13-4-5", -22)]
    [InlineData("3*4*5", 60)]
    [InlineData("120/4/3", 10)]
    [InlineData("120/4/3/3", 3)]
    [InlineData("12*10-4*3*3-12*8/4", 12 * 10 - 4 * 3 * 3 - 12 * 8 / 4)]
    [InlineData("13 - (4-5)", 14)]
    [InlineData("(120-15-3) / ( 13-2-2 )  /  ( 5-1-1 )", 102 / 27)]
    [MemberData(nameof(AboveMaxDepthExpressions))]
    public void ExpressionIsEvaluatedCorrectly(string expression, int expectedResult)
    {
        MockRandomWrapper random = new();

        var parseTree = Parser.Parse(expression);
        parseTree.Error.ShouldBeNullOrEmpty(expression);
        var result = parseTree.Evaluate(random, out var working);
        result.ShouldBe(expectedResult);

        // The working should also be a valid expression with the same result:
        // (This only holds for expressions without dice or named integers)
        var workingParseTree = Parser.Parse(working);
        workingParseTree.Error.ShouldBeNullOrEmpty(working);
        var workingResult = workingParseTree.Evaluate(random, out _);
        workingResult.ShouldBe(expectedResult);
    }

    [Theory]
    [MemberData(nameof(BelowMaxDepthExpressions))]
    public void OverLargeExpressionIsRejected(string expression)
    {
        var parseTree = Parser.Parse(expression);
        parseTree.Error.ShouldNotBeNullOrEmpty(expression);
    }

    [Theory]
    [InlineData("1d20", 4, 20, 4)]
    [InlineData("1d20", 7, 20, 7)]
    [InlineData("4d20 - 2d6", 32,
        20, 3,
        20, 6,
        20, 18,
        20, 12,
        6, 5,
        6, 2)]
    [InlineData("4d20k1", 18,
        20, 1, 20, 4, 20, 9, 20, 18)]
    [InlineData("4d20k1", 18,
        20, 18, 20, 9, 20, 4, 20, 1)]
    [InlineData("4d20k1", 18,
        20, 18, 20, 4, 20, 4, 20, 18)]
    [InlineData("4d20k1", 18,
        20, 18, 20, 16, 20, 18, 20, 18)]
    [InlineData("4d20k1", 18,
        20, 18, 20, 4, 20, 4, 20, 4)]
    [InlineData("4d20l1", 1,
        20, 1, 20, 4, 20, 9, 20, 18)]
    [InlineData("4d20l1", 1,
        20, 18, 20, 9, 20, 4, 20, 1)]
    [InlineData("4d20l1", 4,
        20, 18, 20, 4, 20, 4, 20, 18)]
    [InlineData("4d20l1", 16,
        20, 18, 20, 16, 20, 18, 20, 18)]
    [InlineData("4d20l1", 4,
        20, 18, 20, 4, 20, 4, 20, 4)]
    [InlineData("4d20k2", 27,
        20, 1, 20, 4, 20, 9, 20, 18)]
    [InlineData("4d20k2", 27,
        20, 18, 20, 9, 20, 4, 20, 1)]
    [InlineData("4d20k2", 36,
        20, 18, 20, 4, 20, 4, 20, 18)]
    [InlineData("4d20k2", 36,
        20, 18, 20, 16, 20, 18, 20, 18)]
    [InlineData("4d20k2", 22,
        20, 18, 20, 4, 20, 4, 20, 4)]
    [InlineData("4d20l2", 5,
        20, 1, 20, 4, 20, 9, 20, 18)]
    [InlineData("4d20l2", 5,
        20, 18, 20, 9, 20, 4, 20, 1)]
    [InlineData("4d20l2", 8,
        20, 18, 20, 4, 20, 4, 20, 18)]
    [InlineData("4d20l2", 34,
        20, 18, 20, 16, 20, 18, 20, 18)]
    [InlineData("4d20l2", 8,
        20, 18, 20, 4, 20, 4, 20, 4)]
    public void DiceAreRolledAsExpected(string expression, int expectedResult, params int[] randomExpectations)
    {
        MockRandomWrapper random = new(randomExpectations);

        var parseTree = Parser.Parse(expression);
        parseTree.Error.ShouldBeNullOrEmpty(expression);
        var result = parseTree.Evaluate(random, out _);
        result.ShouldBe(expectedResult);

        random.AssertCompleted();
    }
}
