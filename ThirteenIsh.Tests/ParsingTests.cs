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
    [InlineData("+5", 5)]
    [InlineData("-11", -11)]
    [InlineData("3+4+5", 12)]
    [InlineData("13-4-5", 4)]
    [InlineData("+13+4++5", 22)]
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
        parseTree.ParseError.ShouldBeNullOrEmpty(expression);
        var result = parseTree.Evaluate(random, out var working);
        result.ShouldBe(expectedResult);

        // The working should also be a valid expression with the same result:
        // (This only holds for expressions without dice or named integers)
        var workingParseTree = Parser.Parse(working);
        workingParseTree.ParseError.ShouldBeNullOrEmpty(working);
        var workingResult = workingParseTree.Evaluate(random, out _);
        workingResult.ShouldBe(expectedResult);
    }

    [Theory]
    [MemberData(nameof(BelowMaxDepthExpressions))]
    public void OverLargeExpressionIsRejected(string expression)
    {
        var parseTree = Parser.Parse(expression);
        parseTree.ParseError.ShouldNotBeNullOrEmpty(expression);
    }

    [Theory]
    [InlineData("1d6", 3, 6, 3)]
    [InlineData("3d6", 15, 6, 6, 6, 5, 6, 4)]
    [InlineData("d6", 2, 6, 2)]
    [InlineData("2d8 + 2d6", 12, 8, 4, 8, 5, 6, 2, 6, 1)]
    [InlineData("2d8 + d6", 11, 8, 4, 8, 5, 6, 2)]
    [InlineData("+2d8+d6", 11, 8, 4, 8, 5, 6, 2)]
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
    [InlineData("4d8", 14,
        8, 2, 8, 3, 8, 4, 8, 5)]
    [InlineData("+4d8", 14,
        8, 2, 8, 3, 8, 4, 8, 5)]
    [InlineData("-4d8", -15,
        8, 2, 8, 3, 8, 4, 8, 6)]
    public void DiceAreRolledAsExpected(string expression, int expectedResult, params int[] randomExpectations)
    {
        MockRandomWrapper random = new(randomExpectations);

        var parseTree = Parser.Parse(expression);
        parseTree.ParseError.ShouldBeNullOrEmpty(expression);
        var result = parseTree.Evaluate(random, out _);
        result.ShouldBe(expectedResult);

        random.AssertCompleted();
    }

    // This should work and not take outrageously long
    [Fact]
    public void LargeDiceRollIsOkay()
    {
        const string expression = "100d10000k99";
        MockRandomWrapper random = new(EnumerateRandomExpectations().ToArray());

        var parseTree = Parser.Parse(expression);
        parseTree.ParseError.ShouldBeNullOrEmpty(expression);

        var result = parseTree.Evaluate(random, out _);
        result.ShouldBe(Enumerable.Range(2, 99).Select(i => i * 17).Sum());

        IEnumerable<int> EnumerateRandomExpectations()
        {
            for (var i = 1; i <= 100; ++i)
            {
                yield return 10000; // die size
                yield return i * 17; // result of roll
            }
        }
    }

    [Theory]
    [InlineData("101d100")]
    [InlineData("99d10001")]
    [InlineData("99d99k100")]
    [InlineData("99d99l100")]
    [InlineData("0d10")]
    [InlineData("42d0")]
    [InlineData("-5d-4")]
    [InlineData("2d10k0")]
    [InlineData("2d10l0")]
    [InlineData("-2d10k-2")]
    [InlineData("-2d10l-3")]
    public void OverLargeDiceRollsDoNotParseSuccessfully(string expression)
    {
        var parseTree = Parser.Parse(expression);
        parseTree.ParseError.ShouldNotBeNullOrEmpty();

        // check with a valid prefix and suffix too :)
        var parseTree2 = Parser.Parse($"10 + {expression} + 4d8");
        parseTree2.ParseError.ShouldNotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("-1d6", -3, 6, 3)]
    [InlineData("-d6", -5, 6, 5)]
    [InlineData("+d6", 4, 6, 4)]
    [InlineData("5--d8", 12, 8, 7)]
    [InlineData("5--2d8", 18, 8, 4, 8, 9)]
    [InlineData("1+-d6", -1, 6, 2)]
    [InlineData("1-+d6", -1, 6, 2)]
    [InlineData("1--d6", 7, 6, 6)]
    [InlineData("2d6--d8", 15, 6, 3, 6, 5, 8, 7)]
    [InlineData("2d6+-d8", 1, 6, 3, 6, 5, 8, 7)]
    [InlineData("1d8--3", 10, 8, 7)]
    [InlineData("1d8--1d6", 12, 8, 7, 6, 5)]
    [InlineData("0-(-3)", 3)]
    [InlineData("0-(-1d6)", 4, 6, 4)]
    public void NegativeDiceExpressionsWork(string expression, int expectedResult, params int[] randomExpectations)
    {
        MockRandomWrapper random = new(randomExpectations);

        var parseTree = Parser.Parse(expression);
        parseTree.ParseError.ShouldBeNullOrEmpty(expression);
        var result = parseTree.Evaluate(random, out _);
        result.ShouldBe(expectedResult);

        random.AssertCompleted();
    }

    [Theory]
    [InlineData("--1d6")]
    [InlineData("---1d6")]
    [InlineData("--d6")]
    [InlineData("+-1d6")]
    [InlineData("-+1d6")]
    public void ChainedUnaryNegationsAreNotValid(string expression)
    {
        var parseTree = Parser.Parse(expression);
        parseTree.ParseError.ShouldNotBeNullOrEmpty(expression);
    }
}
