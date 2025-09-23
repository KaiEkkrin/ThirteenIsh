using Shouldly;
using System.Text;
using ThirteenIsh.Game;

namespace ThirteenIsh.Tests.Game;

public class TableRowTests
{
    [Fact]
    public void TableCell_Constructor_SetsPropertiesCorrectly()
    {
        var cell = new TableCell("Test Text", true);

        cell.Text.ShouldBe("Test Text");
        cell.RightJustify.ShouldBeTrue();
    }

    [Fact]
    public void TableCell_DefaultConstructor_LeftJustifiesByDefault()
    {
        var cell = new TableCell("Test");

        cell.Text.ShouldBe("Test");
        cell.RightJustify.ShouldBeFalse();
    }

    [Fact]
    public void TableCell_Empty_HasEmptyText()
    {
        TableCell.Empty.Text.ShouldBe(string.Empty);
        TableCell.Empty.RightJustify.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(42, "42")]
    [InlineData(-15, "-15")]
    [InlineData(999, "999")]
    public void TableCell_FromNumber_CreatesRightJustifiedCell(int value, string expectedText)
    {
        var cell = TableCell.FromNumber(value);

        cell.Text.ShouldBe(expectedText);
        cell.RightJustify.ShouldBeTrue();
    }

    [Fact]
    public void TableRow_CellCount_ReturnsCorrectCount()
    {
        var row = new TableRow(new TableCell("A"), new TableCell("B"), new TableCell("C"));

        row.CellCount.ShouldBe(3);
    }

    [Fact]
    public void TableRow_EmptyRow_HasZeroCells()
    {
        var row = new TableRow();

        row.CellCount.ShouldBe(0);
    }

    [Fact]
    public void TableRow_ContributeMaxCellSizes_CalculatesCorrectSizes()
    {
        var row = new TableRow(
            new TableCell("Short"),
            new TableCell("Very Long Text"),
            new TableCell("Med")
        );
        var maxCellSizes = new int[3];

        row.ContributeMaxCellSizes(maxCellSizes);

        maxCellSizes[0].ShouldBe(5); // "Short"
        maxCellSizes[1].ShouldBe(14); // "Very Long Text"
        maxCellSizes[2].ShouldBe(3); // "Med"
    }

    [Fact]
    public void TableRow_ContributeMaxCellSizes_UpdatesExistingMaxes()
    {
        var row = new TableRow(new TableCell("NewLonger"), new TableCell("X"));
        var maxCellSizes = new int[] { 5, 10 }; // Existing sizes

        row.ContributeMaxCellSizes(maxCellSizes);

        maxCellSizes[0].ShouldBe(9); // Updated from 5 to 9 ("NewLonger")
        maxCellSizes[1].ShouldBe(10); // Unchanged (10 > 1)
    }

    [Fact]
    public void TableRow_ContributeMaxCellSizes_ThrowsOnSizeMismatch()
    {
        var row = new TableRow(new TableCell("A"), new TableCell("B"));
        var maxCellSizes = new int[3]; // Wrong size

        Should.Throw<InvalidOperationException>(() => row.ContributeMaxCellSizes(maxCellSizes));
    }

    [Fact]
    public void TableRow_Append_ReturnsCharacterCount()
    {
        var row = new TableRow(new TableCell("ABC"), new TableCell("XY"));
        var builder = new StringBuilder();
        var maxCellSizes = new int[] { 5, 3 };

        var characterCount = row.Append(builder, maxCellSizes, '·');

        characterCount.ShouldBeGreaterThan(0);
        characterCount.ShouldBe(builder.Length);
    }

    [Fact]
    public void TableRow_Append_IncludesAllCellContent()
    {
        var row = new TableRow(new TableCell("Name"), new TableCell("Value"));
        var builder = new StringBuilder();
        var maxCellSizes = new int[] { 6, 8 };

        row.Append(builder, maxCellSizes, '·');
        var result = builder.ToString();

        result.ShouldContain("Name");
        result.ShouldContain("Value");
    }

    [Fact]
    public void TableRow_Append_HandlesRightJustification()
    {
        var row = new TableRow(
            new TableCell("Left", false),
            new TableCell("Right", true)
        );
        var builder = new StringBuilder();
        var maxCellSizes = new int[] { 8, 8 };

        row.Append(builder, maxCellSizes, '·');
        var result = builder.ToString();

        result.ShouldContain("Left");
        result.ShouldContain("Right");
        // Both should be present, but we don't check exact positioning
    }

    [Fact]
    public void TableRow_Append_HandlesFewerCellsThanMaxSizes()
    {
        var row = new TableRow(new TableCell("OnlyCell"));
        var builder = new StringBuilder();
        var maxCellSizes = new int[] { 10, 5, 3 }; // More columns than cells

        var characterCount = row.Append(builder, maxCellSizes, '·');
        var result = builder.ToString();

        characterCount.ShouldBeGreaterThan(0);
        result.ShouldContain("OnlyCell");
    }

    [Fact]
    public void TableRow_Append_HandlesPaddingCharacter()
    {
        var row = new TableRow(new TableCell("A"), new TableCell("B"));
        var builder = new StringBuilder();
        var maxCellSizes = new int[] { 5, 5 };

        row.Append(builder, maxCellSizes, '*');
        var result = builder.ToString();

        result.ShouldContain("A");
        result.ShouldContain("B");
        // Should use the padding character somewhere
        result.ShouldContain("*");
    }

    [Fact]
    public void TableRow_Append_ReplacesSpacesWithNonBreakingSpaces()
    {
        var row = new TableRow(new TableCell("Multi Word Text"));
        var builder = new StringBuilder();
        var maxCellSizes = new int[] { 20 };

        row.Append(builder, maxCellSizes, '·');
        var result = builder.ToString();

        // Should contain non-breaking spaces instead of regular spaces
        result.ShouldContain("Multi\u00a0Word\u00a0Text");
        result.ShouldNotContain("Multi Word Text");
    }
}