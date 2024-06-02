using LoremNET;
using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;

namespace ThirteenIsh.Tests;

/// <summary>
/// Exercises the various helper methods for managing combatants in encounters.
/// </summary>
public class EncounterTests
{
    private readonly Encounter _encounter = new()
    {
        AdventureName = "Test",
        ChannelId = 0,
        Round = 1
    };

    private readonly HashSet<int> _numbersDrawn = [];

    [Fact]
    public void EmptyEncounterHasNoCombatants()
    {
        _encounter.Combatants.ShouldBeEmpty();
        _encounter.CombatantsInTurnOrder.ShouldBeEmpty();
        _encounter.NextTurn(out _).ShouldBeNull();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(15, false)]
    [InlineData(15, true)]
    public void CombatantsCanBeInsertedInOrder(int count, bool asMonster)
    {
        List<string> combatantNames = [];
        for (var i = 0; i < count; ++i)
        {
            var name = $"{i}{Lorem.Words(1)}";
            var combatant = CreateTestCombatant(name, 20 - i, asMonster);
            _encounter.InsertCombatantIntoTurnOrder(combatant);
            combatantNames.Add(name);
        }

        _encounter.Combatants.Select(c => c.Alias).Order().ShouldBe(combatantNames.Order());
        _encounter.CombatantsInTurnOrder.Select(c => c.Alias).ShouldBe(combatantNames);
        TestTurnOrder(combatantNames);
    }

    // Adding a new combatant at the same initiative as an existing one is meant to place them first
    [Theory]
    [InlineData(1, false, false)]
    [InlineData(1, false, true)]
    [InlineData(15, false, false)]
    [InlineData(15, false, true)]
    [InlineData(15, true, false)]
    [InlineData(15, true, true)]
    public void CombatantsCanBeInsertedInReverseOrder(int count, bool addAtSameInitiative, bool asMonster)
    {
        List<string> combatantNames = [];
        for (var i = 0; i < count; ++i)
        {
            var name = $"{i}{Lorem.Words(1)}";
            var combatant = CreateTestCombatant(name, addAtSameInitiative ? 20 : i, asMonster);
            _encounter.InsertCombatantIntoTurnOrder(combatant);
            combatantNames.Insert(0, name);
        }

        _encounter.Combatants.Select(c => c.Alias).Order().ShouldBe(combatantNames.Order());
        _encounter.CombatantsInTurnOrder.Select(c => c.Alias).ShouldBe(combatantNames);
        TestTurnOrder(combatantNames);
    }

    [Theory]
    [InlineData(1, 293848972)]
    [InlineData(15, 234897892)]
    [InlineData(15, 892734894)]
    [InlineData(15, 347456982)]
    [InlineData(15, 982374958)]
    public void MixedCombatantsCanBeInsertedRandomly(int count, int seed)
    {
        Random random = new(seed);
        List<CombatantBase> combatants = [];
        for (var i = 0; i < count; ++i)
        {
            var name = $"{i}{Lorem.Words(1)}";
            var combatant = CreateTestCombatant(name, DrawUniqueInt(random), random.Next(2) == 1);
            _encounter.InsertCombatantIntoTurnOrder(combatant);
            combatants.Add(combatant);
        }

        _encounter.Combatants.OrderBy(c => c.Alias).ShouldBe(combatants.OrderBy(c => c.Alias));
        _encounter.CombatantsInTurnOrder.ShouldBe(combatants.OrderByDescending(c => c.Initiative));
        TestTurnOrder(_encounter.CombatantsInTurnOrder.Select(c => c.Alias).ToList());
    }

    [Theory]
    [InlineData(234897892)]
    [InlineData(892734894)]
    [InlineData(347456982)]
    [InlineData(982374958)]
    public void CombatantsCanBeInsertedBeforeOthers(int seed)
    {
        CombatantBase[] pass1 = [
            CreateTestCombatant("A1", 20, false),
            CreateTestCombatant("B1", 19, true),
            CreateTestCombatant("C1", 18, false),
            CreateTestCombatant("D1", 16, false),
            CreateTestCombatant("E1", 15, true),
            ];

        CombatantBase[] pass2 = [
            CreateTestCombatant("A2", 20, false),
            CreateTestCombatant("B2", 19, true),
            CreateTestCombatant("C2", 18, true),
            CreateTestCombatant("D2", 16, false),
            CreateTestCombatant("E2", 15, true),
            ];

        CombatantBase[] pass3 = [
            CreateTestCombatant("A3", 20, true),
            CreateTestCombatant("B3", 19, false),
            CreateTestCombatant("C3", 18, false),
            CreateTestCombatant("D3", 16, true),
            CreateTestCombatant("E3", 15, false),
            ];

        // The order of doing each pass shouldn't matter -- what should matter is only the order
        // of passes
        Random random = new(seed);
        random.Shuffle(pass1);
        random.Shuffle(pass2);
        random.Shuffle(pass3);

        foreach (var combatant in pass1.Concat(pass2).Concat(pass3)) _encounter.InsertCombatantIntoTurnOrder(combatant);

        List<string> expectedOrder = ["A3", "A2", "A1", "B3", "B2", "B1", "C3", "C2", "C1", "D3", "D2", "D1", "E3", "E2", "E1"];

        _encounter.CombatantsInTurnOrder.Select(c => c.Alias).ShouldBe(expectedOrder);
        _encounter.CombatantsInTurnOrder.Select(c => (c.Initiative, c.InitiativeAdjustment))
            .Distinct()
            .Count()
            .ShouldBe(15);

        TestTurnOrder(expectedOrder);

        // This is a good time to test some removals, I think
        _encounter.RemoveCombatant("A3").ShouldBe(CombatantRemoveResult.Success);
        _encounter.RemoveCombatant("C2").ShouldBe(CombatantRemoveResult.Success);
        _encounter.RemoveCombatant("E2").ShouldBe(CombatantRemoveResult.Success);
        _encounter.RemoveCombatant("E1").ShouldBe(CombatantRemoveResult.IsTheirTurn);
        _encounter.RemoveCombatant("C2").ShouldBe(CombatantRemoveResult.NotFound);

        expectedOrder.Remove("A3");
        expectedOrder.Remove("C2");
        expectedOrder.Remove("E2");

        _encounter.CombatantsInTurnOrder.Select(c => c.Alias).ShouldBe(expectedOrder);
        TestTurnOrder(expectedOrder, true);

        // If I move on by one more turn, I should be able to remove E1
        _encounter.NextTurn(out var newRound).ShouldNotBeNull().Alias.ShouldBe("A2");
        newRound.ShouldBeTrue();
        _encounter.RemoveCombatant("E1").ShouldBe(CombatantRemoveResult.Success);
        expectedOrder.Remove("E1");

        _encounter.CombatantsInTurnOrder.Select(c => c.Alias).ShouldBe(expectedOrder);
    }

    private static CombatantBase CreateTestCombatant(string nameAlias, int initiative, bool asMonster) => asMonster
        ? new MonsterCombatant()
        {
            Alias = nameAlias,
            Initiative = initiative,
            LastUpdated = DateTimeOffset.Now,
            Name = nameAlias,
            Sheet = new CharacterSheet(),
            UserId = 0
        }
        : new AdventurerCombatant()
        {
            Alias = nameAlias,
            Initiative = initiative,
            Name = nameAlias,
            UserId = 0
        };

    private int DrawUniqueInt(Random random)
    {
        while (true)
        {
            var number = random.Next();
            if (_numbersDrawn.Add(number)) return number;
        }
    }

    private void TestTurnOrder(IReadOnlyCollection<string> expectedNameAliasesInOrder, bool afterStart = false)
    {
        // Try this twice, to make sure that we restart from the beginning correctly.
        var expectNewRound = afterStart;
        for (var i = 0; i < 2; ++i)
        {
            foreach (var nameAlias in expectedNameAliasesInOrder)
            {
                var combatant = _encounter.NextTurn(out var newRound);
                combatant.ShouldNotBeNull().Alias.ShouldBe(nameAlias);
                newRound.ShouldBe(expectNewRound);
                expectNewRound = false;
            }

            expectNewRound = true;
        }
    }
}
