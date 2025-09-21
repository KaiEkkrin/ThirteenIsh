using Shouldly;
using System.Collections.Immutable;
using ThirteenIsh.Game;

namespace ThirteenIsh.Tests.Game;

public class GamePropertyGroupBuilderTests
{
    [Fact]
    public void Constructor_SetsGroupName()
    {
        var builder = new GamePropertyGroupBuilder("Test Group");

        // We can't directly access the name until Build() is called
        var group = builder.Build();
        group.GroupName.ShouldBe("Test Group");
    }

    [Fact]
    public void AddProperty_SingleProperty_IncludesInGroup()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var builder = new GamePropertyGroupBuilder("Test Group");

        builder.AddProperty(property);
        var group = builder.Build();

        group.Properties.ShouldContain(property);
        group.Properties.Count.ShouldBe(1);
    }

    [Fact]
    public void AddProperty_MultipleProperties_IncludesAllInGroup()
    {
        var property1 = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var property2 = new GameProperty("Race", new[] { "Human", "Elf" }, false);
        var builder = new GamePropertyGroupBuilder("Test Group");

        builder.AddProperty(property1).AddProperty(property2);
        var group = builder.Build();

        group.Properties.ShouldContain(property1);
        group.Properties.ShouldContain(property2);
        group.Properties.Count.ShouldBe(2);
    }

    [Fact]
    public void AddProperty_ReturnsBuilderForFluentInterface()
    {
        var property = new GameProperty("Class", new[] { "Fighter" }, false);
        var builder = new GamePropertyGroupBuilder("Test Group");

        var result = builder.AddProperty(property);

        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void AddProperties_MultiplePropertiesAtOnce_IncludesAllInGroup()
    {
        var property1 = new GameProperty("Class", new[] { "Fighter" }, false);
        var property2 = new GameProperty("Race", new[] { "Human" }, false);
        var property3 = new GameProperty("Background", new[] { "Noble" }, false);
        var builder = new GamePropertyGroupBuilder("Test Group");

        builder.AddProperties(property1, property2, property3);
        var group = builder.Build();

        group.Properties.ShouldContain(property1);
        group.Properties.ShouldContain(property2);
        group.Properties.ShouldContain(property3);
        group.Properties.Count.ShouldBe(3);
    }

    [Fact]
    public void AddProperties_ReturnsBuilderForFluentInterface()
    {
        var property1 = new GameProperty("Class", new[] { "Fighter" }, false);
        var property2 = new GameProperty("Race", new[] { "Human" }, false);
        var builder = new GamePropertyGroupBuilder("Test Group");

        var result = builder.AddProperties(property1, property2);

        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void OrderByName_SortsPropertiesByName()
    {
        var propertyZ = new GameProperty("Zebra", new[] { "Value" }, false);
        var propertyA = new GameProperty("Alpha", new[] { "Value" }, false);
        var propertyM = new GameProperty("Middle", new[] { "Value" }, false);
        var builder = new GamePropertyGroupBuilder("Test Group");

        builder.AddProperties(propertyZ, propertyA, propertyM).OrderByName();
        var group = builder.Build();

        group.Properties[0].Name.ShouldBe("Alpha");
        group.Properties[1].Name.ShouldBe("Middle");
        group.Properties[2].Name.ShouldBe("Zebra");
    }

    [Fact]
    public void OrderByName_ReturnsBuilderForFluentInterface()
    {
        var builder = new GamePropertyGroupBuilder("Test Group");

        var result = builder.OrderByName();

        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void Build_EmptyBuilder_CreatesEmptyGroup()
    {
        var builder = new GamePropertyGroupBuilder("Empty Group");

        var group = builder.Build();

        group.GroupName.ShouldBe("Empty Group");
        group.Properties.ShouldBeEmpty();
    }

    [Fact]
    public void Build_CreatesImmutableGroup()
    {
        var property = new GameProperty("Class", new[] { "Fighter" }, false);
        var builder = new GamePropertyGroupBuilder("Test Group");
        builder.AddProperty(property);

        var group = builder.Build();

        // Verify that the group properties are immutable
        group.Properties.ShouldBeAssignableTo<ImmutableList<GamePropertyBase>>();
    }

    [Fact]
    public void FluentInterface_CombinesAllOperations()
    {
        var propertyC = new GameProperty("Class", new[] { "Fighter" }, false);
        var propertyB = new GameProperty("Background", new[] { "Noble" }, false);
        var propertyR = new GameProperty("Race", new[] { "Human" }, false);

        var group = new GamePropertyGroupBuilder("Test Group")
            .AddProperty(propertyC)
            .AddProperties(propertyB, propertyR)
            .OrderByName()
            .Build();

        group.GroupName.ShouldBe("Test Group");
        group.Properties.Count.ShouldBe(3);
        group.Properties[0].Name.ShouldBe("Background");
        group.Properties[1].Name.ShouldBe("Class");
        group.Properties[2].Name.ShouldBe("Race");
    }

    [Fact]
    public void Build_CanBeCalledMultipleTimes()
    {
        var property = new GameProperty("Class", new[] { "Fighter" }, false);
        var builder = new GamePropertyGroupBuilder("Test Group");
        builder.AddProperty(property);

        var group1 = builder.Build();
        var group2 = builder.Build();

        // Both should have the same content but be different instances
        group1.GroupName.ShouldBe(group2.GroupName);
        group1.Properties.Count.ShouldBe(group2.Properties.Count);
        group1.ShouldNotBeSameAs(group2);
    }
}