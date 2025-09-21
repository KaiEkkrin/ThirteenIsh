using Shouldly;
using ThirteenIsh.Game;

namespace ThirteenIsh.Tests.Game;

public class GameResultTypesTests
{
    [Fact]
    public void GameCounterRollResult_ErrorMessage_NotRollable_ReturnsCorrectMessage()
    {
        var result = new GameCounterRollResult
        {
            CounterName = "TestCounter",
            Error = GameCounterRollError.NotRollable
        };

        result.ErrorMessage.ShouldBe("TestCounter is not rollable");
    }

    [Fact]
    public void GameCounterRollResult_ErrorMessage_NoValue_ReturnsCorrectMessage()
    {
        var result = new GameCounterRollResult
        {
            CounterName = "TestCounter",
            Error = GameCounterRollError.NoValue
        };

        result.ErrorMessage.ShouldBe("No value defined for TestCounter");
    }

    [Fact]
    public void GameCounterRollResult_ErrorMessage_Success_ReturnsEmptyString()
    {
        var result = new GameCounterRollResult
        {
            CounterName = "TestCounter",
            Error = GameCounterRollError.Success
        };

        result.ErrorMessage.ShouldBe(string.Empty);
    }

    [Fact]
    public void EncounterRollResult_BuildError_ValidErrorResult_CreatesEncounterResult()
    {
        var rollResult = new GameCounterRollResult
        {
            CounterName = "Initiative",
            Error = GameCounterRollError.NotRollable,
            Roll = 10,
            Success = false,
            Working = "1d20"
        };

        var encounterResult = EncounterRollResult.BuildError(rollResult);

        encounterResult.CounterName.ShouldBe("Initiative");
        encounterResult.Error.ShouldBe(GameCounterRollError.NotRollable);
        encounterResult.Roll.ShouldBe(10);
        encounterResult.Success.ShouldBe(false);
        encounterResult.Working.ShouldBe("1d20");
        encounterResult.Alias.ShouldBe(string.Empty);
    }

    [Fact]
    public void EncounterRollResult_BuildError_SuccessResult_ThrowsException()
    {
        var rollResult = new GameCounterRollResult
        {
            CounterName = "Initiative",
            Error = GameCounterRollError.Success
        };

        Should.Throw<ArgumentException>(() => EncounterRollResult.BuildError(rollResult));
    }

    [Fact]
    public void EncounterRollResult_BuildSuccess_ValidSuccessResult_CreatesEncounterResult()
    {
        var rollResult = new GameCounterRollResult
        {
            CounterName = "Initiative",
            Error = GameCounterRollError.Success,
            Roll = 15,
            Success = true,
            Working = "1d20+3"
        };

        var encounterResult = EncounterRollResult.BuildSuccess(rollResult, "Goblin1");

        encounterResult.CounterName.ShouldBe("Initiative");
        encounterResult.Error.ShouldBe(GameCounterRollError.Success);
        encounterResult.Roll.ShouldBe(15);
        encounterResult.Success.ShouldBe(true);
        encounterResult.Working.ShouldBe("1d20+3");
        encounterResult.Alias.ShouldBe("Goblin1");
    }

    [Fact]
    public void EncounterRollResult_BuildSuccess_ErrorResult_ThrowsException()
    {
        var rollResult = new GameCounterRollResult
        {
            CounterName = "Initiative",
            Error = GameCounterRollError.NotRollable
        };

        Should.Throw<ArgumentException>(() => EncounterRollResult.BuildSuccess(rollResult, "Goblin1"));
    }

    [Fact]
    public void EncounterRollResult_BuildSuccess_EmptyAlias_ThrowsException()
    {
        var rollResult = new GameCounterRollResult
        {
            CounterName = "Initiative",
            Error = GameCounterRollError.Success
        };

        Should.Throw<ArgumentException>(() => EncounterRollResult.BuildSuccess(rollResult, string.Empty));
    }

    [Fact]
    public void EncounterRollResult_BuildSuccess_NullAlias_ThrowsException()
    {
        var rollResult = new GameCounterRollResult
        {
            CounterName = "Initiative",
            Error = GameCounterRollError.Success
        };

        Should.Throw<ArgumentException>(() => EncounterRollResult.BuildSuccess(rollResult, null!));
    }

    [Theory]
    [InlineData(GameCounterRollError.NotRollable)]
    [InlineData(GameCounterRollError.NoValue)]
    public void EncounterRollResult_BuildError_AllErrorTypes_Work(GameCounterRollError errorType)
    {
        var rollResult = new GameCounterRollResult
        {
            CounterName = "TestCounter",
            Error = errorType
        };

        var encounterResult = EncounterRollResult.BuildError(rollResult);

        encounterResult.Error.ShouldBe(errorType);
        encounterResult.CounterName.ShouldBe("TestCounter");
    }

    [Fact]
    public void EncounterRollResult_InheritsFromGameCounterRollResult()
    {
        var rollResult = new GameCounterRollResult
        {
            CounterName = "Initiative",
            Error = GameCounterRollError.Success
        };

        var encounterResult = EncounterRollResult.BuildSuccess(rollResult, "TestAlias");

        encounterResult.ShouldBeAssignableTo<GameCounterRollResult>();
    }

    [Fact]
    public void GameCounterRollResult_CanBeCreatedWithMinimalProperties()
    {
        var result = new GameCounterRollResult
        {
            CounterName = "Test",
            Error = GameCounterRollError.Success
        };

        result.CounterName.ShouldBe("Test");
        result.Error.ShouldBe(GameCounterRollError.Success);
        result.Roll.ShouldBe(0); // Default value
        result.Success.ShouldBeNull(); // Default value
        result.Working.ShouldBe(string.Empty); // Default value
    }
}