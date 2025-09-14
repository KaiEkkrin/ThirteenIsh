using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.Swn;

internal class SwnSystem : GameSystem
{
    public const string SystemName = "Stars Without Number";

    public const string Basics = "Basics";
    public const string Attributes = "Attributes";
    public const string Skills = "Skills";
    public const string PsychicSkills = "Psychic Skills";
    public const string General = "General";
    public const string SavingThrows = "Saving Throws";
    public const string Equipment = "Equipment";

    public const string Expert = "Expert";
    public const string Psychic = "Psychic";
    public const string Warrior = "Warrior";

    public const string Level = "Level";

    public const string Strength = "Strength";
    public const string Dexterity = "Dexterity";
    public const string Constitution = "Constitution";
    public const string Intelligence = "Intelligence";
    public const string Wisdom = "Wisdom";
    public const string Charisma = "Charisma";

    public const string Administer = "Administer";
    public const string Connect = "Connect";
    public const string Exert = "Exert";
    public const string Fix = "Fix";
    public const string Heal = "Heal";
    public const string Know = "Know";
    public const string Lead = "Lead";
    public const string Notice = "Notice";
    public const string Perform = "Perform";
    public const string Pilot = "Pilot";
    public const string Program = "Program";
    public const string Punch = "Punch";
    public const string Shoot = "Shoot";
    public const string Sneak = "Sneak";
    public const string Stab = "Stab";
    public const string Survive = "Survive";
    public const string Talk = "Talk";
    public const string Trade = "Trade";
    public const string Work = "Work";

    public const string Biopsionics = "Biopsionics";
    public const string Metapsionics = "Metapsionics";
    public const string Precognition = "Precognition";
    public const string Telekinesis = "Telekinesis";
    public const string Telepathy = "Telepathy";
    public const string Teleportation = "Teleportation";

    public const string ArmorClass = "Armor Class";
    public const string ArmorClassAlias = "AC";
    public const string ArmorValue = "Armor Value";
    public const string AttackBonus = "Attack Bonus";
    public const string Effort = "Effort";
    public const string HitPoints = "Hit Points";
    public const string HitPointsAlias = "HP";

    public const string Evasion = "Evasion";
    public const string Mental = "Mental";
    public const string Physical = "Physical";

    private SwnSystem(string name, IEnumerable<CharacterSystem> characterSystems) : base(name, characterSystems)
    {
    }

    public static SwnSystem Build()
    {
        // Add two of the same for a full class, or two different ones for an Adventurer with the
        // matching partial classes.
        GameProperty class1Property = new("Class 1", [Expert, Psychic, Warrior], true);
        GameProperty class2Property = new("Class 2", [Expert, Psychic, Warrior], true);
        GameCounter levelCounter = new(Level, defaultValue: 1, minValue: 1);

        var basics = new GamePropertyGroupBuilder(Basics)
            .AddProperties(class1Property, class2Property, levelCounter)
            .Build();

        GamePropertyGroupBuilder attributesBuilder = new(Attributes);
        var strength = BuildAttribute(attributesBuilder, Strength);
        var dexterity = BuildAttribute(attributesBuilder, Dexterity);
        var constitution = BuildAttribute(attributesBuilder, Constitution);
        var intelligence = BuildAttribute(attributesBuilder, Intelligence);
        var wisdom = BuildAttribute(attributesBuilder, Wisdom);
        var charisma = BuildAttribute(attributesBuilder, Charisma);
        var attributes = attributesBuilder.Build();

        GamePropertyGroupBuilder skillsBuilder = new(Skills);
        BuildSkill(skillsBuilder, Administer);
        BuildSkill(skillsBuilder, Connect);
        BuildSkill(skillsBuilder, Exert);
        BuildSkill(skillsBuilder, Fix);
        BuildSkill(skillsBuilder, Heal);
        BuildSkill(skillsBuilder, Know);
        BuildSkill(skillsBuilder, Lead);
        BuildSkill(skillsBuilder, Notice);
        BuildSkill(skillsBuilder, Perform);
        BuildSkill(skillsBuilder, Pilot);
        BuildSkill(skillsBuilder, Program);
        BuildSkill(skillsBuilder, Punch);
        BuildSkill(skillsBuilder, Shoot);
        BuildSkill(skillsBuilder, Sneak);
        BuildSkill(skillsBuilder, Stab);
        BuildSkill(skillsBuilder, Survive);
        BuildSkill(skillsBuilder, Talk);
        BuildSkill(skillsBuilder, Trade);
        BuildSkill(skillsBuilder, Work);
        var skills = skillsBuilder.Build();

        GamePropertyGroupBuilder psychicSkillsBuilder = new(PsychicSkills);
        var biopsionics = BuildSkill(psychicSkillsBuilder, Biopsionics);
        var metapsionics = BuildSkill(psychicSkillsBuilder, Metapsionics);
        var precognition = BuildSkill(psychicSkillsBuilder, Precognition);
        var telekinesis = BuildSkill(psychicSkillsBuilder, Telekinesis);
        var telepathy = BuildSkill(psychicSkillsBuilder, Telepathy);
        var teleportation = BuildSkill(psychicSkillsBuilder, Teleportation);
        var psychicSkills = psychicSkillsBuilder.Build();

        // Players can fix Armor Value to the base AC of their armor so that the actual AC counter
        // has their Dexterity bonus added automatically.
        GameCounter armorValueCounter = new(ArmorValue, defaultValue: 10, minValue: 10, maxValue: 21);
        
        var equipment = new GamePropertyGroupBuilder(Equipment)
            .AddProperty(armorValueCounter)
            .Build();

        ArmorClassCounter armorClassCounter = new(armorValueCounter, dexterity);
        AttackBonusCounter attackBonusCounter = new(class1Property, class2Property, levelCounter);
        EffortCounter effortCounter = new(class1Property, class2Property, constitution, wisdom,
            biopsionics, metapsionics, precognition, telekinesis, telepathy, teleportation);

        HitPointsCounter hitPointsCounter = new(class1Property, class2Property, levelCounter, constitution);

        var general = new GamePropertyGroupBuilder(General)
            .AddProperty(armorClassCounter)
            .AddProperty(attackBonusCounter)
            .AddProperty(effortCounter)
            .AddProperty(hitPointsCounter)
            .Build();

        SavingThrowCounter evasion = new(Evasion, dexterity, intelligence);
        SavingThrowCounter mental = new(Mental, wisdom, charisma);
        SavingThrowCounter physical = new(Physical, strength, constitution);
        var savingThrows = new GamePropertyGroupBuilder(SavingThrows)
            .AddProperties(evasion, mental, physical)
            .Build();

        // TODO build a monster system as well (make sure I can make characters successfully first)
        SwnCharacterSystem playerCharacterSystem = new([basics, attributes, skills, psychicSkills, general, savingThrows, equipment]);
        return new SwnSystem(SystemName, [playerCharacterSystem]);
    }

    public override EncounterRollResult EncounterAdd(DataContext dataContext, Character character, Encounter encounter,
        NameAliasCollection nameAliasCollection, IRandomWrapper random, int rerolls, int swarmCount, ulong userId)
    {
        throw new NotImplementedException();
    }

    public override void EncounterBegin(Encounter encounter)
    {
        // No special setup needed at the start of an encounter
    }

    public override EncounterRollResult EncounterJoin(DataContext dataContext, Adventurer adventurer, Encounter encounter,
        NameAliasCollection nameAliasCollection, IRandomWrapper random, int rerolls, ulong userId)
    {
        // In Stars Without Number, the initiative roll is 1d8 + Dexterity bonus.
        var dexterityBonusCounter = GetCharacterSystem(CharacterType.PlayerCharacter)
            .GetProperty<GameCounter>(adventurer.Sheet, AttributeBonusCounter.GetBonusCounterName(Dexterity));

        ParseTreeBase parseTree = DiceRollParseTree.BuildWithRerolls(8, 0, 1);
        var dexterityBonus = dexterityBonusCounter.GetValue(adventurer);
        if (dexterityBonus.HasValue)
        {
            parseTree = new BinaryOperationParseTree(0, parseTree, new IntegerParseTree(
                0, dexterityBonus.Value, dexterityBonusCounter.Name), '+');
        }

        var initiative = parseTree.Evaluate(random, out var working);
        GameCounterRollResult initiativeRollResult = new()
        {
            CounterName = "Initiative",
            Error = GameCounterRollError.Success,
            Roll = initiative,
            Working = working
        };

        AdventurerCombatant combatant = new()
        {
            Alias = nameAliasCollection.Add(adventurer.Name, 10, false),
            Initiative = initiative,
            InitiativeRollWorking = working,
            Name = adventurer.Name,
            UserId = userId
        };

        encounter.InsertCombatantIntoTurnOrder(combatant);
        return EncounterRollResult.BuildSuccess(initiativeRollResult, combatant.Alias);
    }

    public override string GetCharacterSummary(CharacterSheet sheet, CharacterType type)
    {
        var characterSystem = GetCharacterSystem(type);
        switch (type)
        {
            case CharacterType.PlayerCharacter:
                var class1 = characterSystem.GetProperty<GameProperty>(sheet, "Class 1").GetValue(sheet);
                var class2 = characterSystem.GetProperty<GameProperty>(sheet, "Class 2").GetValue(sheet);
                var level = characterSystem.GetProperty<GameCounter>(sheet, Level).GetValue(sheet);

                // Build class description - handle dual-class characters
                string classDescription;
                if (string.IsNullOrEmpty(class1) && string.IsNullOrEmpty(class2))
                {
                    classDescription = "Adventurer";
                }
                else if (class1 == class2 && !string.IsNullOrEmpty(class1))
                {
                    // Full class (e.g., "Expert/Expert" becomes "Expert")
                    classDescription = class1;
                }
                else if (!string.IsNullOrEmpty(class1) && !string.IsNullOrEmpty(class2))
                {
                    // Dual class (e.g., "Expert/Warrior")
                    classDescription = $"{class1}/{class2}";
                }
                else
                {
                    // Single partial class
                    classDescription = $"Partial {class1 ?? class2}";
                }

                return $"Level {level} {classDescription}";

            case CharacterType.Monster:
                // TODO: Add monster type display once monster system is implemented
                return "SWN Monster";

            default:
                throw new ArgumentException("Unrecognised character type", nameof(type));
        }
    }

    protected override CombatantBase? EncounterNextRound(Encounter encounter, IRandomWrapper random)
    {
        // Nothing special to do here
        return encounter.GetCurrentCombatant();
    }

    private static AttributeBonusCounter BuildAttribute(GamePropertyGroupBuilder builder, string attributeName)
    {
        GameAbilityCounter attributeCounter = new(attributeName);
        AttributeBonusCounter bonusCounter = new(attributeCounter);
        builder.AddProperty(attributeCounter).AddProperty(bonusCounter);
        return bonusCounter;
    }

    public static GameCounter BuildSkill(GamePropertyGroupBuilder builder, string skillName)
    {
        SkillCounter skillCounter = new(skillName);
        builder.AddProperty(skillCounter);
        return skillCounter;
    }
}
