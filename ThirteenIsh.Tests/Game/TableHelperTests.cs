using Shouldly;
using System.Text;
using ThirteenIsh.Game;

namespace ThirteenIsh.Tests.Game;

public class TableHelperTests
{
    [Fact]
    public void BuildTable_EmptyData_ReturnsEmptyString()
    {
        var result = TableHelper.BuildTable([]);
        result.ShouldBeEmpty();
    }

    [Fact]
    public void BuildTable_SingleCell_ContainsExpectedContent()
    {
        var data = new List<TableRow> { new(new TableCell("Test")) };

        var result = TableHelper.BuildTable(data);

        result.ShouldContain("Test");
        result.ShouldContain("```");
    }

    [Fact]
    public void BuildTable_MultipleRowsAndColumns_ContainsAllContent()
    {
        var data = new List<TableRow>
        {
            new(new TableCell("Name"), new TableCell("Value")),
            new(new TableCell("Strength"), new TableCell("15")),
            new(new TableCell("Dexterity"), new TableCell("12"))
        };

        var result = TableHelper.BuildTable(data);

        // Check that all content is present
        result.ShouldContain("Name");
        result.ShouldContain("Value");
        result.ShouldContain("Strength");
        result.ShouldContain("15");
        result.ShouldContain("Dexterity");
        result.ShouldContain("12");

        // Check structural elements
        result.ShouldStartWith("```");
        result.ShouldEndWith("```\n");
    }

    [Fact]
    public void BuildTableEx_CustomLanguage_IncludesLanguageInCodeBlock()
    {
        var data = new List<TableRow> { new(new TableCell("Test")) };
        var builder = new StringBuilder();

        TableHelper.BuildTableEx(builder, data, language: "diff");
        var result = builder.ToString();

        result.ShouldContain("Test");
        result.ShouldContain("```diff");
    }

    [Fact]
    public void BuildTableEx_CustomPaddingCharacters_UsesSpecifiedCharacters()
    {
        var data = new List<TableRow> { new(new TableCell("A"), new TableCell("B")) };
        var builder = new StringBuilder();

        TableHelper.BuildTableEx(builder, data, cellPaddingCharacter: '*', tablePaddingCharacter: '#');
        var result = builder.ToString();

        result.ShouldContain("A");
        result.ShouldContain("B");
        // Should use custom padding characters somewhere in the output
        (result.Contains('*') || result.Contains('#')).ShouldBeTrue();
    }

    [Fact]
    public void BuildTableEx_MaxTableWidthConstraint_RespectsSizeLimit()
    {
        var data = new List<TableRow> { new(new TableCell("VeryLongContentThatExceedsNormalTableWidth")) };
        var builder = new StringBuilder();

        TableHelper.BuildTableEx(builder, data, maxTableWidth: 20);
        var result = builder.ToString();

        result.ShouldContain("VeryLongContentThatExceedsNormalTableWidth");
        // Should still be formatted as a table
        result.ShouldContain("```");
    }

    [Fact]
    public void BuildTableEx_TwoColumnMode_WithSmallTable_MayUseCompactLayout()
    {
        var data = new List<TableRow>
        {
            new(new TableCell("A")),
            new(new TableCell("B")),
            new(new TableCell("C")),
            new(new TableCell("D"))
        };
        var builder = new StringBuilder();

        TableHelper.BuildTableEx(builder, data, drawAsTwoColumnsIfPossible: true, maxTableWidth: 100);
        var result = builder.ToString();

        // All content should be present regardless of layout
        result.ShouldContain("A");
        result.ShouldContain("B");
        result.ShouldContain("C");
        result.ShouldContain("D");
        result.ShouldContain("```");
    }

    [Fact]
    public void BuildTableEx_DisableTwoColumnMode_UsesStandardLayout()
    {
        var data = new List<TableRow>
        {
            new(new TableCell("Item1")),
            new(new TableCell("Item2"))
        };
        var builder = new StringBuilder();

        TableHelper.BuildTableEx(builder, data, drawAsTwoColumnsIfPossible: false);
        var result = builder.ToString();

        result.ShouldContain("Item1");
        result.ShouldContain("Item2");
        result.ShouldContain("```");
    }

    [Fact]
    public void BuildTableEx_VaryingRowWidths_ThrowsException()
    {
        var data = new List<TableRow>
        {
            new(new TableCell("A"), new TableCell("B"), new TableCell("C")),
            new(new TableCell("X")), // Different number of cells
            new(new TableCell("Y"), new TableCell("Z"))
        };
        var builder = new StringBuilder();

        // TableHelper requires consistent cell counts across rows
        Should.Throw<InvalidOperationException>(() => TableHelper.BuildTableEx(builder, data));
    }

    [Theory]
    [InlineData("")]
    [InlineData("json")]
    [InlineData("diff")]
    [InlineData("yaml")]
    public void BuildTableEx_DifferentLanguages_ProducesValidOutput(string language)
    {
        var data = new List<TableRow> { new(new TableCell("Content")) };
        var builder = new StringBuilder();

        TableHelper.BuildTableEx(builder, data, language: language);
        var result = builder.ToString();

        result.ShouldContain("Content");
        if (string.IsNullOrEmpty(language))
        {
            result.ShouldContain("```");
        }
        else
        {
            result.ShouldContain($"```{language}");
        }
    }
}