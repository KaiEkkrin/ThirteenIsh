

namespace ThirteenIsh.Game.Dragonbane;

/// <summary>
/// Describes the Dragonbane game system.
/// </summary>
internal sealed class DragonbaneSystem : GameSystem
{
    public const string SystemName = "Dragonbane";

    public const string Basics = "Basics";
    public const string Kin = "Kin";
    public const string Profession = "Profession";

    public const string Attributes = "Attributes";
    public const string DerivedRatings = "Derived Ratings";
    public const string CoreSkills = "Core Skills";
    public const string WeaponSkills = "Weapon Skills";
    public const string SecondarySkills = "Secondary Skills";
    public const string Equipment = "Equipment";

    public const string HitPoints = "Hit Points";
    public const string HitPointsAlias = "HP";

    public const string WillpowerPoints = "Willpower Points";
    public const string WillpowerPointsAlias = "WP";

    public const string StrengthDamageBonus = "Strength Damage Bonus";
    public const string AgilityDamageBonus = "Agility Damage Bonus";

    public const string Human = "Human";
    public const string Halfling = "Halfling";
    public const string Dwarf = "Dwarf";
    public const string Elf = "Elf";
    public const string Mallard = "Mallard";
    public const string Wolfkin = "Wolfkin";

    public const string Artisan = "Artisan";
    public const string Bard = "Bard";
    public const string Fighter = "Fighter";
    public const string Hunter = "Hunter";
    public const string Knight = "Knight";
    public const string Mage = "Mage";
    public const string Mariner = "Mariner";
    public const string Merchant = "Merchant";
    public const string Scholar = "Scholar";
    public const string Thief = "Thief";

    public const string Strength = "Strength";
    public const string Constitution = "Constitution";
    public const string Agility = "Agility";
    public const string Intelligence = "Intelligence";
    public const string Willpower = "Willpower";
    public const string Charisma = "Charisma";

    // We store the un-drawn cards of the initiative deck as a bit field in this encounter variable
    private const string InitiativeDeck = "InitiativeDeck";

    private DragonbaneSystem(params CharacterSystem[] characterSystems) : base(SystemName, characterSystems)
    {
    }

    public static GameSystem Build()
    {
        GamePropertyGroupBuilder basicsBuilder = new(Basics);

        GameProperty kinProperty = new(Kin, [Human, Halfling, Dwarf, Elf, Mallard, Wolfkin], true);
        GameProperty professionProperty = new(Profession, [Artisan, Bard, Fighter, Hunter, Knight,
            Mage, Mariner, Merchant, Scholar, Thief], true);

        basicsBuilder.AddProperties(kinProperty, professionProperty);

        GamePropertyGroupBuilder attributesBuilder = new(Attributes);

        GameAbilityCounter strengthCounter = new(Strength, maxValue: 18);
        GameAbilityCounter constitutionCounter = new(Constitution, maxValue: 18);
        GameAbilityCounter agilityCounter = new(Agility, maxValue: 18);
        GameAbilityCounter intelligenceCounter = new(Intelligence, maxValue: 18);
        GameAbilityCounter willpowerCounter = new(Willpower, maxValue: 18);
        GameAbilityCounter charismaCounter = new(Charisma, maxValue: 18);

        attributesBuilder.AddProperties(strengthCounter, constitutionCounter, agilityCounter, intelligenceCounter,
            willpowerCounter, charismaCounter);

        GamePropertyGroupBuilder derivedRatingsBuilder = new(DerivedRatings);

        MovementCounter movementCounter = new(kinProperty, agilityCounter);
        PointsCounter hitPointsCounter = new(HitPoints, HitPointsAlias, constitutionCounter);
        PointsCounter willpowerPointsCounter = new(WillpowerPoints, WillpowerPointsAlias, willpowerCounter);

        DamageBonusCounter strengthDamageBonusCounter = new(StrengthDamageBonus, strengthCounter);
        DamageBonusCounter agilityDamageBonusCounter = new(AgilityDamageBonus, agilityCounter);

        derivedRatingsBuilder.AddProperties(movementCounter,
            hitPointsCounter, willpowerPointsCounter, strengthDamageBonusCounter, agilityDamageBonusCounter);

        GamePropertyGroupBuilder coreSkillsBuilder = new(CoreSkills);
        GamePropertyGroupBuilder weaponSkillsBuilder = new(WeaponSkills);
        GamePropertyGroupBuilder secondarySkillsBuilder = new(SecondarySkills);

        BuildSkill(coreSkillsBuilder, "Acrobatics", agilityCounter);
        BuildSkill(coreSkillsBuilder, "Awareness", intelligenceCounter);
        BuildSkill(coreSkillsBuilder, "Bartering", charismaCounter);
        BuildSkill(coreSkillsBuilder, "Beast Lore", intelligenceCounter);
        BuildSkill(coreSkillsBuilder, "Bluffing", charismaCounter);
        BuildSkill(coreSkillsBuilder, "Bushcraft", intelligenceCounter);
        BuildSkill(coreSkillsBuilder, "Crafting", strengthCounter);
        BuildSkill(coreSkillsBuilder, "Evade", agilityCounter);
        BuildSkill(coreSkillsBuilder, "Healing", intelligenceCounter);
        BuildSkill(coreSkillsBuilder, "Hunting & Fishing", agilityCounter);
        BuildSkill(coreSkillsBuilder, "Languages", intelligenceCounter);
        BuildSkill(coreSkillsBuilder, "Myths & Legends", intelligenceCounter);
        BuildSkill(coreSkillsBuilder, "Performance", charismaCounter);
        BuildSkill(coreSkillsBuilder, "Persuasion", charismaCounter);
        BuildSkill(coreSkillsBuilder, "Riding", agilityCounter);
        BuildSkill(coreSkillsBuilder, "Seamanship", intelligenceCounter);
        BuildSkill(coreSkillsBuilder, "Sleight of Hand", agilityCounter);
        BuildSkill(coreSkillsBuilder, "Sneaking", agilityCounter);
        BuildSkill(coreSkillsBuilder, "Spot Hidden", intelligenceCounter);
        BuildSkill(coreSkillsBuilder, "Swimming", agilityCounter);

        BuildSkill(weaponSkillsBuilder, "Axes", strengthCounter);
        BuildSkill(weaponSkillsBuilder, "Bows", agilityCounter);
        BuildSkill(weaponSkillsBuilder, "Brawling", strengthCounter);
        BuildSkill(weaponSkillsBuilder, "Crossbows", agilityCounter);
        BuildSkill(weaponSkillsBuilder, "Hammers", strengthCounter);
        BuildSkill(weaponSkillsBuilder, "Knives", agilityCounter);
        BuildSkill(weaponSkillsBuilder, "Slings", agilityCounter);
        BuildSkill(weaponSkillsBuilder, "Spears", strengthCounter);
        BuildSkill(weaponSkillsBuilder, "Staves", agilityCounter);
        BuildSkill(weaponSkillsBuilder, "Swords", strengthCounter);

        BuildSkill(secondarySkillsBuilder, "Animism", intelligenceCounter, true);
        BuildSkill(secondarySkillsBuilder, "Elementalism", intelligenceCounter, true);
        BuildSkill(secondarySkillsBuilder, "Mentalism", intelligenceCounter, true);

        // The variables to these track durability.
        // Players will need to make a custom counter for weapon durability ;)
        var equipmentBuilder = new GamePropertyGroupBuilder(Equipment)
            .AddProperty(new GameCounter("Armor", options: GameCounterOptions.HasVariable))
            .AddProperty(new GameCounter("Helmet", options: GameCounterOptions.HasVariable));

        var playerCharacterSystem = new DragonbaneCharacterSystemBuilder(CharacterType.PlayerCharacter, SystemName)
            .AddPropertyGroup(basicsBuilder)
            .AddPropertyGroup(attributesBuilder)
            .AddPropertyGroup(derivedRatingsBuilder)
            .AddPropertyGroup(coreSkillsBuilder)
            .AddPropertyGroup(weaponSkillsBuilder)
            .AddPropertyGroup(secondarySkillsBuilder)
            .AddPropertyGroup(equipmentBuilder)
            .Build();

        // TODO declare monster system here

        return new DragonbaneSystem(playerCharacterSystem);
    }

    private static void BuildSkill(GamePropertyGroupBuilder builder, string name, GameAbilityCounter attributeCounter,
        bool secondary = false)
    {
        GameCounter skillCounter = new(name, maxValue: 13);
        SkillLevelCounter skillLevelCounter = new(attributeCounter, skillCounter, secondary);
        builder.AddProperties(skillCounter, skillLevelCounter);
    }

    public override EncounterRollResult EncounterAdd(
        Character character,
        Encounter encounter,
        NameAliasCollection nameAliasCollection,
        IRandomWrapper random,
        int rerolls,
        int swarmCount,
        ulong userId)
    {
        // TODO implement this
        throw new NotImplementedException();
    }

    public override void EncounterBegin(Encounter encounter)
    {
        ResetInitiativeDeck(encounter);
    }

    public override EncounterRollResult EncounterJoin(
        Adventurer adventurer,
        Encounter encounter,
        NameAliasCollection nameAliasCollection,
        IRandomWrapper random,
        int rerolls,
        ulong userId)
    {
        // TODO -- support surprise
        var card = DrawInitiativeDeck(encounter, random, out var working);
        if (!card.HasValue) return new EncounterRollResult { CounterName = "Initiative", Error = GameCounterRollError.NoValue };

        AdventurerCombatant newCombatant = new()
        {
            Alias = nameAliasCollection.Add(adventurer.Name, 10, false),
            Initiative = card.Value,
            InitiativeRollWorking = working,
            Name = adventurer.Name,
            UserId = userId
        };

        encounter.InsertCombatantIntoTurnOrder(newCombatant);
        return new EncounterRollResult
        {
            Alias = newCombatant.Alias,
            CounterName = "Initiative",
            Error = GameCounterRollError.Success,
            Roll = card.Value,
            Working = working
        };
    }

    public override string GetCharacterSummary(CharacterSheet sheet, CharacterType type)
    {
        var characterSystem = GetCharacterSystem(type);
        var kin = characterSystem.GetProperty<GameProperty>(sheet, Kin).GetValue(sheet);
        var profession = characterSystem.GetProperty<GameProperty>(sheet, Profession).GetValue(sheet);
        return $"{kin} {profession}";
    }

    protected override CombatantBase? EncounterNextRound(Encounter encounter, IRandomWrapper random)
    {
        // When we roll over to the next round, re-draw the initiative.
        ResetInitiativeDeck(encounter);
        foreach (var combatant in encounter.Combatants)
        {
            var card = DrawInitiativeDeck(encounter, random, out _);
            if (!card.HasValue) break; // TODO ???

            combatant.Initiative = card.Value;
            combatant.InitiativeAdjustment = 0;
        }

        var currentCombatant = encounter.Combatants.Min(CombatantTurnOrderComparer.Instance);
        encounter.TurnAlias = currentCombatant?.Alias;
        return currentCombatant;
    }

    private static int? DrawInitiativeDeck(Encounter encounter, IRandomWrapper random, out string working)
    {
        var deck = encounter.State.Counters.TryGetValue(InitiativeDeck, out var deckValue) ? deckValue : 0;
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

        encounter.State.Counters.SetValue(InitiativeDeck, deck & ~(1 << cardIndex));
        return card;
    }

    private static void ResetInitiativeDeck(Encounter encounter)
    {
        encounter.State.Counters.SetValue(InitiativeDeck, (1 << 10) - 1);
    }
}
