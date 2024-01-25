using Shouldly;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Tests;

// TODO maybe mock the dice roller itself and verify that too?
// For now just checking the rest of the expression stuff
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
    [InlineData("3+4+5", 12)]
    [InlineData("13-4-5", 4)]
    [InlineData("3*4*5", 60)]
    [InlineData("120/4/3", 10)]
    [InlineData("120/4/3/3", 3)]
    [InlineData("12*10-4*3*3-12*8/4", 12 * 10 - 4 * 3 * 3 - 12 * 8 / 4)]
    [InlineData("13 - (4-5)", 14)]
    [InlineData("(120-15-3) / ( 13-2-2 )  /  ( 5-1-1 )", 102 / 27)]
    [MemberData(nameof(AboveMaxDepthExpressions))]
    public void ExpressionIsEvaluatedCorrectly(string expression, int expectedResult)
    {
        var parseTree = Parser.Parse(expression);
        parseTree.Error.ShouldBeNullOrEmpty(expression);
        var result = parseTree.Evaluate(out var working);
        result.ShouldBe(expectedResult);

        // The working should also be a valid expression with the same result:
        var workingParseTree = Parser.Parse(working);
        workingParseTree.Error.ShouldBeNullOrEmpty(working);
        var workingResult = workingParseTree.Evaluate(out _);
        workingResult.ShouldBe(expectedResult);
    }

    [Theory]
    [MemberData(nameof(BelowMaxDepthExpressions))]
    public void OverLargeExpressionIsRejected(string expression)
    {
        var parseTree = Parser.Parse(expression);
        parseTree.Error.ShouldNotBeNullOrEmpty(expression);
    }
}
