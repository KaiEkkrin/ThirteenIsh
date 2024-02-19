using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.Dragonbane;

internal sealed class DragonbaneLogic(
    GameProperty kinProperty,
    GameProperty professionProperty
    ) : GameSystemLogicBase
{
    // We store the un-drawn cards of the initiative deck as a bit field in this encounter variable
    private const string InitiativeDeck = "InitiativeDeck";

    public override void EncounterBegin(Encounter encounter)
    {
        ResetInitiativeDeck(encounter);
    }

    public override GameCounterRollResult? EncounterJoin(
        Adventurer adventurer,
        Encounter encounter,
        IRandomWrapper random,
        int rerolls,
        ulong userId)
    {
        // TODO -- support surprise
        var card = DrawInitiativeDeck(encounter, random, out var working);
        if (!card.HasValue) return null;

        encounter.AddCombatant(new AdventurerCombatant
        {
            Initiative = card.Value,
            Name = adventurer.Name,
            UserId = (long)userId
        });

        return new GameCounterRollResult { Roll = card.Value, Working = working };
    }

    public override string GetCharacterSummary(CharacterSheet sheet)
    {
        var kin = kinProperty.GetValue(sheet);
        var profession = professionProperty.GetValue(sheet);
        return $"{kin} {profession}";
    }

    private static int? DrawInitiativeDeck(Encounter encounter, IRandomWrapper random, out string working)
    {
        var deck = encounter.Variables[InitiativeDeck];
        List<int> cards = [];
        for (var i = 0; i < 10; ++i)
        {
            if ((deck & (1 << i)) != 0) cards.Add(i);
        }

        if (cards.Count == 0)
        {
            working = string.Empty;
            return null;
        }

        var cardIndex = random.NextInteger(0, cards.Count);
        var card = cards[cardIndex];
        working = "🎲 " + string.Join(", ", cards.Select((c, i) => i == cardIndex ? $"{c}" : $"~~{c}~~"));

        encounter.Variables[InitiativeDeck] = deck & ~(1 << cardIndex);
        return card;
    }

    private static void ResetInitiativeDeck(Encounter encounter)
    {
        encounter.Variables[InitiativeDeck] = (1 << 10) - 1;
    }
}
