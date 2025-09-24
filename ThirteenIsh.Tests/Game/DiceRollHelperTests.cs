using Shouldly;
using ThirteenIsh.Game;

namespace ThirteenIsh.Tests.Game;

/// <summary>
/// Unit tests for DiceRollHelper functionality, particularly for extracting natural d20 roll values
/// from working strings in various scenarios including rerolls.
/// </summary>
public class DiceRollHelperTests
{
    [Fact]
    public void ExtractNaturalD20Roll_BasicRoll_ReturnsCorrectValue()
    {
        // Arrange
        var working = "1d20 🎲 15";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(15);
    }

    [Fact]
    public void ExtractNaturalD20Roll_RollWithPositiveBonus_ReturnsD20Value()
    {
        // Arrange
        var working = "1d20 🎲 15 + 3";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(15);
    }

    [Fact]
    public void ExtractNaturalD20Roll_RollWithNegativeBonus_ReturnsD20Value()
    {
        // Arrange
        var working = "1d20 🎲 8 + -2";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(8);
    }

    [Fact]
    public void ExtractNaturalD20Roll_Natural1_ReturnsOne()
    {
        // Arrange
        var working = "1d20 🎲 1";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(1);
    }

    [Fact]
    public void ExtractNaturalD20Roll_Natural20_ReturnsTwenty()
    {
        // Arrange
        var working = "1d20 🎲 20";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(20);
    }

    [Fact]
    public void ExtractNaturalD20Roll_Natural20WithBonus_ReturnsTwenty()
    {
        // Arrange
        var working = "1d20 🎲 20 + 5";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(20);
    }

    [Fact]
    public void ExtractNaturalD20Roll_Natural1WithBonus_ReturnsOne()
    {
        // Arrange
        var working = "1d20 🎲 1 + 5";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(1);
    }

    [Fact]
    public void ExtractNaturalD20Roll_TwoRerolls_ReturnsKeptValue()
    {
        // Arrange - First roll was 5 (struck through), final kept roll is 18
        var working = "1d20 🎲 18 [~~5~~ + 18]";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(18);
    }

    [Fact]
    public void ExtractNaturalD20Roll_ThreeRerolls_ReturnsKeptValue()
    {
        // Arrange - First two rolls were 3 and 7 (struck through), final kept roll is 15
        var working = "1d20 🎲 15 [~~3~~ + ~~7~~ + 15]";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(15);
    }

    [Fact]
    public void ExtractNaturalD20Roll_RerollsWithNatural20_ReturnsTwenty()
    {
        // Arrange - First roll was 2 (struck through), final kept roll is natural 20
        var working = "1d20 🎲 20 [~~2~~ + 20]";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(20);
    }

    [Fact]
    public void ExtractNaturalD20Roll_RerollsWithNatural1_ReturnsOne()
    {
        // Arrange - First roll was 15 (struck through), final kept roll is natural 1
        var working = "1d20 🎲 1 [~~15~~ + 1]";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(1);
    }

    [Fact]
    public void ExtractNaturalD20Roll_RerollsWithBonus_ReturnsNaturalRoll()
    {
        // Arrange - First roll was 8 (struck through), final kept roll is 12, plus bonus
        var working = "1d20 🎲 12 [~~8~~ + 12] + 3";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(12);
    }

    [Fact]
    public void ExtractNaturalD20Roll_MultipleRerollsWithBonus_ReturnsNaturalRoll()
    {
        // Arrange - Two rerolls (4, 9 struck through), final kept roll is 16, plus bonus
        var working = "1d20 🎲 16 [~~4~~ + ~~9~~ + 16] + 2";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(16);
    }

    [Fact]
    public void ExtractNaturalD20Roll_EmptyString_ThrowsArgumentException()
    {
        // Arrange
        var working = "";

        // Act & Assert
        Should.Throw<ArgumentException>(() => DiceRollHelper.ExtractNaturalD20Roll(working));
    }

    [Fact]
    public void ExtractNaturalD20Roll_NullString_ThrowsArgumentException()
    {
        // Arrange
        string? working = null;

        // Act & Assert
        Should.Throw<ArgumentException>(() => DiceRollHelper.ExtractNaturalD20Roll(working!));
    }

    [Fact]
    public void ExtractNaturalD20Roll_InvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var working = "This is not a dice roll";

        // Act & Assert
        Should.Throw<ArgumentException>(() => DiceRollHelper.ExtractNaturalD20Roll(working));
    }

    [Fact]
    public void ExtractNaturalD20Roll_NotD20Roll_ThrowsArgumentException()
    {
        // Arrange - This is a d6 roll, not d20
        var working = "1d6 🎲 4";

        // Act & Assert
        Should.Throw<ArgumentException>(() => DiceRollHelper.ExtractNaturalD20Roll(working));
    }

    [Fact]
    public void ExtractNaturalD20Roll_MultipleD20Rolls_ReturnsFirstD20()
    {
        // Arrange - Edge case with multiple d20s in working string
        var working = "1d20 🎲 12 + 1d20 🎲 8";

        // Act
        var result = DiceRollHelper.ExtractNaturalD20Roll(working);

        // Assert
        result.ShouldBe(12); // Should return the first d20 result
    }
}