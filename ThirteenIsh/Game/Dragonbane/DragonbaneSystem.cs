namespace ThirteenIsh.Game.Dragonbane;

/// <summary>
/// Describes the Dragonbane game system.
/// </summary>
internal static class DragonbaneSystem
{
    public const string Basics = "Basics";
    public const string Attributes = "Attributes";
    public const string DerivedRatings = "DerivedRatings";
    public const string CoreSkills = "Core Skills";
    public const string SecondarySkills = "Secondary Skills";
    public const string Equipment = "Equipment";

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

    public static GameSystem Build()
    {
        GamePropertyGroupBuilder basicsBuilder = new(Basics);

        GameProperty kinProperty = new("Kin", [Human, Halfling, Dwarf, Elf, Mallard, Wolfkin]);
        GameProperty professionProperty = new("Profession", [Artisan, Bard, Fighter, Hunter, Knight,
            Mage, Mariner, Merchant, Scholar, Thief]);

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
        PointsCounter hitPointsCounter = new("Hit Points", "HP", constitutionCounter);
        PointsCounter willpowerPointsCounter = new("Willpower Points", "WP", willpowerCounter);

        DamageBonusCounter strengthDamageBonusCounter = new("Strength Damage Bonus", strengthCounter);
        DamageBonusCounter agilityDamageBonusCounter = new("Agility Damage Bonus", agilityCounter);

        derivedRatingsBuilder.AddProperties(movementCounter,
            hitPointsCounter, willpowerPointsCounter, strengthDamageBonusCounter, agilityDamageBonusCounter);

        GamePropertyGroupBuilder coreSkillsBuilder = new(CoreSkills);
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

        BuildSkill(coreSkillsBuilder, "Axes", strengthCounter);
        BuildSkill(coreSkillsBuilder, "Bows", agilityCounter);
        BuildSkill(coreSkillsBuilder, "Brawling", strengthCounter);
        BuildSkill(coreSkillsBuilder, "Crossbows", agilityCounter);
        BuildSkill(coreSkillsBuilder, "Hammers", strengthCounter);
        BuildSkill(coreSkillsBuilder, "Knives", agilityCounter);
        BuildSkill(coreSkillsBuilder, "Slings", agilityCounter);
        BuildSkill(coreSkillsBuilder, "Spears", strengthCounter);
        BuildSkill(coreSkillsBuilder, "Staves", agilityCounter);
        BuildSkill(coreSkillsBuilder, "Swords", strengthCounter);

        BuildSkill(secondarySkillsBuilder, "Animism", intelligenceCounter, true);
        BuildSkill(secondarySkillsBuilder, "Elementalism", intelligenceCounter, true);
        BuildSkill(secondarySkillsBuilder, "Mentalism", intelligenceCounter, true);

        // The variables to these track durability.
        // Players will need to make a custom counter for weapon durability ;)
        var equipmentBuilder = new GamePropertyGroupBuilder(Equipment)
            .AddProperty(new GameCounter("Armor", hasVariable: true))
            .AddProperty(new GameCounter("Helmet", hasVariable: true));

        return new GameSystemBuilder("Dragonbane")
            .AddPropertyGroup(basicsBuilder)
            .AddPropertyGroup(attributesBuilder)
            .AddPropertyGroup(derivedRatingsBuilder)
            .AddPropertyGroup(coreSkillsBuilder)
            .AddPropertyGroup(secondarySkillsBuilder)
            .AddPropertyGroup(equipmentBuilder)
            .Build();
    }

    private static void BuildSkill(GamePropertyGroupBuilder builder, string name, GameAbilityCounter attributeCounter,
        bool secondary = false)
    {
        GameCounter skillCounter = new(name);
        SkillLevelCounter skillLevelCounter = new(attributeCounter, skillCounter, secondary);
        builder.AddProperties(skillCounter, skillLevelCounter);
    }
}
