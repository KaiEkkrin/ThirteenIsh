namespace ThirteenIsh.Game.ThirteenthAge;

/// <summary>
/// Describes the 13th Age game system.
/// </summary>
internal static class ThirteenthAgeSystem
{
    public const string Basics = "Basics";
    public const string AbilityScores = "Ability Scores";
    public const string General = "General";

    private const string Level = "Level";

    private const string Strength = "Strength";
    private const string Dexterity = "Dexterity";
    private const string Constitution = "Constitution";
    private const string Intelligence = "Intelligence";
    private const string Wisdom = "Wisdom";
    private const string Charisma = "Charisma";

    public const string Barbarian = "Barbarian";
    public const string Bard = "Bard";
    public const string Cleric = "Cleric";
    public const string Fighter = "Fighter";
    public const string Paladin = "Paladin";
    public const string Ranger = "Ranger";
    public const string Rogue = "Rogue";
    public const string Sorcerer = "Sorcerer";
    public const string Wizard = "Wizard";

    public static GameSystem Build()
    {
        GameProperty classProperty = new("Class",
                [Barbarian, Bard, Cleric, Fighter, Paladin, Ranger, Rogue, Sorcerer, Wizard],
                true);

        GameCounter levelCounter = new(Level, defaultValue: 1, minValue: 1, maxValue: 10);

        var basicsBuilder = new GamePropertyGroupBuilder(Basics)
            .AddProperties(classProperty, levelCounter);

        GamePropertyGroupBuilder abilityScoresBuilder = new(AbilityScores);

        var strengthBonusCounter = BuildAbility(abilityScoresBuilder, Strength);
        var dexterityBonusCounter = BuildAbility(abilityScoresBuilder, Dexterity);
        var constitutionBonusCounter = BuildAbility(abilityScoresBuilder, Constitution);
        var intelligenceBonusCounter = BuildAbility(abilityScoresBuilder, Intelligence);
        var wisdomBonusCounter = BuildAbility(abilityScoresBuilder, Wisdom);
        var charismaBonusCounter = BuildAbility(abilityScoresBuilder, Charisma);

        var generalBuilder = new GamePropertyGroupBuilder(General)
            .AddProperty(new HitPointsCounter(classProperty, levelCounter, constitutionBonusCounter))
            .AddProperty(new ArmorClassCounter(classProperty, levelCounter, constitutionBonusCounter, dexterityBonusCounter,
                wisdomBonusCounter))
            .AddProperty(new PhysicalDefenseCounter(classProperty, levelCounter, strengthBonusCounter, dexterityBonusCounter,
                constitutionBonusCounter))
            .AddProperty(new MentalDefenseCounter(classProperty, levelCounter, intelligenceBonusCounter, wisdomBonusCounter,
                charismaBonusCounter))
            .AddProperty(new RecoveriesCounter())
            .AddProperty(new RecoveryDieCounter(classProperty));

        return new GameSystemBuilder("13th Age")
            .AddPropertyGroup(basicsBuilder)
            .AddPropertyGroup(abilityScoresBuilder)
            .AddPropertyGroup(generalBuilder)
            .Build(new ThirteenthAgeLogic(classProperty, dexterityBonusCounter, levelCounter));
    }

    private static AbilityBonusCounter BuildAbility(GamePropertyGroupBuilder builder, string abilityName)
    {
        GameAbilityCounter counter = new(abilityName);
        AbilityBonusCounter bonusCounter = new(counter);
        builder.AddProperty(counter).AddProperty(bonusCounter);
        return bonusCounter;
    }
}
