using Shouldly;
using ThirteenIsh.Game;

namespace ThirteenIsh.Tests.Game;

public class GameAbilityCounterTests
{
    [Theory]
    [InlineData("Strength", "STR")]
    [InlineData("Dexterity", "DEX")]
    [InlineData("Constitution", "CON")]
    [InlineData("Intelligence", "INT")]
    [InlineData("Wisdom", "WIS")]
    [InlineData("Charisma", "CHA")]
    public void Constructor_StandardAbilityNames_GeneratesCorrectAlias(string abilityName, string expectedAlias)
    {
        var counter = new GameAbilityCounter(abilityName);

        counter.Name.ShouldBe(abilityName);
        counter.Alias.ShouldBe(expectedAlias);
    }

    [Theory]
    [InlineData("abc", "ABC")]
    [InlineData("Foo", "FOO")]
    [InlineData("BAR", "BAR")]
    public void Constructor_ThreeCharacterNames_GeneratesCorrectAlias(string abilityName, string expectedAlias)
    {
        var counter = new GameAbilityCounter(abilityName);

        counter.Alias.ShouldBe(expectedAlias);
    }

    [Theory]
    [InlineData("VeryLongAttributeName", "VER")]
    [InlineData("AnotherLongName", "ANO")]
    [InlineData("AttributeWithManyCharacters", "ATT")]
    public void Constructor_LongNames_TruncatesToThreeCharacters(string abilityName, string expectedAlias)
    {
        var counter = new GameAbilityCounter(abilityName);

        counter.Alias.ShouldBe(expectedAlias);
        counter.Alias!.Length.ShouldBe(3);
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new GameAbilityCounter(string.Empty));
    }

    [Fact]
    public void Constructor_OneCharacterName_ThrowsException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new GameAbilityCounter("A"));
    }

    [Fact]
    public void Constructor_TwoCharacterName_ThrowsException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new GameAbilityCounter("AB"));
    }

    [Fact]
    public void Constructor_UsesDefaultGameCounterValues()
    {
        var counter = new GameAbilityCounter("Strength");

        counter.DefaultValue.ShouldBe(10);
        counter.MinValue.ShouldBe(1);
        counter.MaxValue.ShouldBe(24);
    }

    [Fact]
    public void Constructor_CanOverrideDefaultValues()
    {
        var counter = new GameAbilityCounter("Strength", defaultValue: 15, minValue: 3, maxValue: 18);

        counter.DefaultValue.ShouldBe(15);
        counter.MinValue.ShouldBe(3);
        counter.MaxValue.ShouldBe(18);
    }

    [Theory]
    [InlineData("strength", "STR")]
    [InlineData("DEXTERITY", "DEX")]
    [InlineData("cOnStItUtIoN", "CON")]
    [InlineData("intelligence", "INT")]
    public void Constructor_DifferentCasing_ProducesUppercaseAlias(string abilityName, string expectedAlias)
    {
        var counter = new GameAbilityCounter(abilityName);

        counter.Alias.ShouldBe(expectedAlias);
    }

    [Fact]
    public void Constructor_InheritsFromGameCounter()
    {
        var counter = new GameAbilityCounter("Strength");

        counter.ShouldBeAssignableTo<GameCounter>();
    }
}