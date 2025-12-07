namespace ThirteenIsh.Game.Swn;

internal class SwnSystem : GameSystem
{
    public const string SystemName = "Stars Without Number";

    public const string PlayerCharacter = "Player Character";
    public const string Monster = "Monster";
    public const string Starship = "Starship";

    public const string Basics = "Basics";
    public const string Attributes = "Attributes";
    public const string AttributeBonuses = "Attribute Bonuses";
    public const string Skills = "Skills";
    public const string PsychicSkills = "Psychic Skills";
    public const string General = "General";
    public const string SavingThrows = "Saving Throws";
    public const string Equipment = "Equipment";
    public const string MonsterStats = "Monster Stats";
    public const string StarshipStats = "Starship Stats";

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

    public const string HitDice = "Hit Dice";
    public const string HitDiceAlias = "HD";
    public const string Morale = "Morale";
    public const string Attack = "Attack";
    public const string Skill = "Skill";
    public const string Save = "Save";

    public const string Armor = "Armor";
    public const string Speed = "Speed";
    public const string Power = "Power";
    public const string Mass = "Mass";
    public const string Crew = "Crew";

    public const string HullClass = "Hull Class";
    public const string Fighter = "Fighter";
    public const string Frigate = "Frigate";
    public const string Cruiser = "Cruiser";
    public const string Capital = "Capital";

    public const string Weapons = "Weapons";

    public const string CommandPoints = "Command Points";
    public const string CommandPointsAlias = "CP";

    private SwnSystem(string name, IEnumerable<CharacterSystem> characterSystems) : base(name, characterSystems)
    {
    }

    public static SwnSystem Build()
    {
        var playerCharacterSystem = BuildPlayerCharacterSystem();
        var monsterCharacterSystem = BuildMonsterCharacterSystem();
        var starshipCharacterSystem = BuildStarshipCharacterSystem();
        return new SwnSystem(SystemName, [playerCharacterSystem, monsterCharacterSystem, starshipCharacterSystem]);
    }

    private static SwnPlayerCharacterSystem BuildPlayerCharacterSystem()
    {
        // Add two of the same for a full class, or two different ones for an Adventurer with the
        // matching partial classes.
        GameProperty class1Property = new("Class 1", [Expert, Psychic, Warrior], true);
        GameProperty class2Property = new("Class 2", [Expert, Psychic, Warrior], true);
        GameCounter levelCounter = new(Level, defaultValue: 1, minValue: 1);

        var basics = new GamePropertyGroupBuilder(Basics)
            .AddProperties(class1Property, class2Property, levelCounter)
            .Build();

        // Create both builders
        GamePropertyGroupBuilder attributesBuilder = new(Attributes);
        GamePropertyGroupBuilder attributeBonusesBuilder = new(AttributeBonuses);

        // BuildAttribute now adds to both groups
        var strength = BuildAttribute(attributesBuilder, attributeBonusesBuilder, Strength);
        var dexterity = BuildAttribute(attributesBuilder, attributeBonusesBuilder, Dexterity);
        var constitution = BuildAttribute(attributesBuilder, attributeBonusesBuilder, Constitution);
        var intelligence = BuildAttribute(attributesBuilder, attributeBonusesBuilder, Intelligence);
        var wisdom = BuildAttribute(attributesBuilder, attributeBonusesBuilder, Wisdom);
        var charisma = BuildAttribute(attributesBuilder, attributeBonusesBuilder, Charisma);

        // Build both groups
        var attributes = attributesBuilder.Build();
        var attributeBonuses = attributeBonusesBuilder.Build();

        // Create attack bonus counter early so it can be passed to skill counters
        AttackBonusCounter attackBonusCounter = new(class1Property, class2Property, levelCounter);

        GamePropertyGroupBuilder skillsBuilder = new(Skills);
        BuildSkill(skillsBuilder, Administer, attackBonusCounter);
        BuildSkill(skillsBuilder, Connect, attackBonusCounter);
        BuildSkill(skillsBuilder, Exert, attackBonusCounter);
        BuildSkill(skillsBuilder, Fix, attackBonusCounter);
        BuildSkill(skillsBuilder, Heal, attackBonusCounter);
        BuildSkill(skillsBuilder, Know, attackBonusCounter);
        BuildSkill(skillsBuilder, Lead, attackBonusCounter);
        BuildSkill(skillsBuilder, Notice, attackBonusCounter);
        BuildSkill(skillsBuilder, Perform, attackBonusCounter);
        BuildSkill(skillsBuilder, Pilot, attackBonusCounter);
        BuildSkill(skillsBuilder, Program, attackBonusCounter);
        BuildSkill(skillsBuilder, Punch, attackBonusCounter);
        BuildSkill(skillsBuilder, Shoot, attackBonusCounter);
        BuildSkill(skillsBuilder, Sneak, attackBonusCounter);
        BuildSkill(skillsBuilder, Stab, attackBonusCounter);
        BuildSkill(skillsBuilder, Survive, attackBonusCounter);
        BuildSkill(skillsBuilder, Talk, attackBonusCounter);
        BuildSkill(skillsBuilder, Trade, attackBonusCounter);
        BuildSkill(skillsBuilder, Work, attackBonusCounter);
        var skills = skillsBuilder.Build();

        GamePropertyGroupBuilder psychicSkillsBuilder = new(PsychicSkills);
        var biopsionics = BuildSkill(psychicSkillsBuilder, Biopsionics, attackBonusCounter);
        var metapsionics = BuildSkill(psychicSkillsBuilder, Metapsionics, attackBonusCounter);
        var precognition = BuildSkill(psychicSkillsBuilder, Precognition, attackBonusCounter);
        var telekinesis = BuildSkill(psychicSkillsBuilder, Telekinesis, attackBonusCounter);
        var telepathy = BuildSkill(psychicSkillsBuilder, Telepathy, attackBonusCounter);
        var teleportation = BuildSkill(psychicSkillsBuilder, Teleportation, attackBonusCounter);
        var psychicSkills = psychicSkillsBuilder.Build();

        // Players can fix Armor Value to the base AC of their armor so that the actual AC counter
        // has their Dexterity bonus added automatically.
        GameCounter armorValueCounter = new(ArmorValue, defaultValue: 10, minValue: 10, maxValue: 21);
        
        var equipment = new GamePropertyGroupBuilder(Equipment)
            .AddProperty(armorValueCounter)
            .Build();

        ArmorClassCounter armorClassCounter = new(armorValueCounter, dexterity);
        EffortCounter effortCounter = new(class1Property, class2Property, constitution, wisdom,
            biopsionics, metapsionics, precognition, telekinesis, telepathy, teleportation);

        HitPointsCounter hitPointsCounter = new(levelCounter, class1Property, class2Property, constitution);

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

        return new SwnPlayerCharacterSystem([basics, attributes, attributeBonuses, skills, psychicSkills, equipment, general, savingThrows]);
    }

    private static SwnMonsterCharacterSystem BuildMonsterCharacterSystem()
    {
        GameCounter hitDiceCounter = new(HitDice, HitDiceAlias, defaultValue: 1, minValue: 1);
        var monsterMorale = new MoraleCounter(Morale);
        var monsterHitPointsCounter = new MonsterHitPointsCounter(hitDiceCounter);

        var monsterStats = new GamePropertyGroupBuilder(MonsterStats)
            .AddProperty(hitDiceCounter)
            .AddProperty(new GameCounter(ArmorClass, ArmorClassAlias))
            .AddProperty(new MonsterAttackCounter(Attack))
            .AddProperty(monsterMorale)
            .AddProperty(new MonsterSkillCounter(Skill))
            .AddProperty(new MonsterSavingThrowCounter())
            .AddProperty(monsterHitPointsCounter)
            .Build();

        return new SwnMonsterCharacterSystem([monsterStats]);
    }

    private static SwnStarshipCharacterSystem BuildStarshipCharacterSystem()
    {
        // This might look a bit weird (obviously starship hit points are not the same thing as
        // player character or monster hit points!) but it makes it fit into our encounter system
        // more easily.
        GameProperty hullClassProperty = new(HullClass, [ Fighter, Frigate, Cruiser, Capital ], true);

        GameCounter hitPointsCounter = new(HitPoints, HitPointsAlias, defaultValue: 1, minValue: 1,
            options: GameCounterOptions.HasVariable);

        GameCounter armorClassCounter = new(ArmorClass, ArmorClassAlias);
        GameCounter armorCounter = new(Armor);
        GameCounter speedCounter = new(Speed);
        MonsterSkillCounter skillCounter = new(Skill, defaultValue: 1);
        GameCounter powerCounter = new(Power, defaultValue: 5, options: GameCounterOptions.HasVariable);
        GameCounter massCounter = new(Mass, defaultValue: 5, options: GameCounterOptions.HasVariable);
        GameCounter crewCounter = new(Crew, defaultValue: 1, options: GameCounterOptions.HasVariable);

        GameCounter commandPointsCounter = new(CommandPoints, CommandPointsAlias, defaultValue: 4,
            options: GameCounterOptions.HasVariable);

        MonsterAttackCounter weaponsCounter = new(Weapons);

        var starshipStats = new GamePropertyGroupBuilder(StarshipStats)
            .AddProperty(hullClassProperty)
            .AddProperty(hitPointsCounter)
            .AddProperty(armorClassCounter)
            .AddProperty(armorCounter)
            .AddProperty(speedCounter)
            .AddProperty(skillCounter)
            .AddProperty(powerCounter)
            .AddProperty(massCounter)
            .AddProperty(crewCounter)
            .AddProperty(commandPointsCounter)
            .AddProperty(weaponsCounter)
            .Build();

        return new SwnStarshipCharacterSystem([starshipStats]);
    }

    public override EncounterRollResult EncounterAdd(Character character, Encounter encounter,
        NameAliasCollection nameAliasCollection, IRandomWrapper random, int rerolls, int swarmCount, ulong userId)
    {
        if (character.CharacterType != CharacterType.Monster)
            throw new ArgumentException("EncounterAdd requires a monster", nameof(character));

        // Set up a new combatant for this monster
        MonsterCombatant combatant = new()
        {
            Alias = nameAliasCollection.Add(character.Name, 5, true),
            CharacterSystemName = character.CharacterSystemName,
            LastUpdated = DateTimeOffset.UtcNow,
            Name = character.Name,
            Sheet = character.Sheet,
            SwarmCount = swarmCount,
            UserId = userId
        };

        var characterSystem = GetCharacterSystem(CharacterType.Monster, character.CharacterSystemName);
        characterSystem.ResetVariables(combatant);

        // In SWN, monsters use 1d8 initiative with no dexterity bonus
        var initiative = RollMonsterInitiative(combatant, encounter, random, rerolls, userId);
        if (initiative.Error != GameCounterRollError.Success)
            return EncounterRollResult.BuildError(initiative);

        // Add it to the encounter
        combatant.Initiative = initiative.Roll;
        combatant.InitiativeRollWorking = initiative.Working;
        encounter.InsertCombatantIntoTurnOrder(combatant);
        return EncounterRollResult.BuildSuccess(initiative, combatant.Alias);
    }

    public override void EncounterBegin(Encounter encounter)
    {
        // No special setup needed at the start of an encounter
    }

    public override EncounterRollResult EncounterJoin(Adventurer adventurer, Encounter encounter,
        NameAliasCollection nameAliasCollection, IRandomWrapper random, int rerolls, ulong userId)
    {
        // In Stars Without Number, the initiative roll is 1d8 + Dexterity bonus.
        // Safely get the Dexterity bonus counter - it may not exist (e.g., for starships)
        var characterSystem = GetCharacterSystem(CharacterType.PlayerCharacter, adventurer.CharacterSystemName);
        var dexterityBonusCounter = characterSystem.GetProperty(AttributeBonusCounter.GetBonusCounterName(Dexterity)) as GameCounter;

        ParseTreeBase parseTree = DiceRollParseTree.BuildWithRerolls(8, 0, 1);

        // Only apply dexterity bonus if the counter exists and has a value
        if (dexterityBonusCounter != null)
        {
            var dexterityBonus = dexterityBonusCounter.GetValue(adventurer);
            if (dexterityBonus.HasValue)
            {
                parseTree = new BinaryOperationParseTree(0, parseTree, new IntegerParseTree(
                    0, dexterityBonus.Value, dexterityBonusCounter.Name), '+');
            }
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
            CharacterSystemName = adventurer.CharacterSystemName,
            Initiative = initiative,
            InitiativeRollWorking = working,
            Name = adventurer.Name,
            UserId = userId
        };

        encounter.InsertCombatantIntoTurnOrder(combatant);
        return EncounterRollResult.BuildSuccess(initiativeRollResult, combatant.Alias);
    }

    public override string GetCharacterSummary(ICharacterBase character)
    {
        var characterSystem = GetCharacterSystem(character.Type, character.CharacterSystemName);

        // Switch based on the actual character system retrieved
        switch (characterSystem.Name)
        {
            case PlayerCharacter:
                var class1 = characterSystem.GetProperty<GameProperty>(character, "Class 1").GetValue(character);
                var class2 = characterSystem.GetProperty<GameProperty>(character, "Class 2").GetValue(character);
                var level = characterSystem.GetProperty<GameCounter>(character, Level).GetValue(character);

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
                    var singleClass = !string.IsNullOrEmpty(class1) ? class1 : class2;
                    classDescription = $"Partial {singleClass}";
                }

                return $"Level {level} {classDescription}";

            case Monster:
                var hitDice = characterSystem.GetProperty<GameCounter>(character, HitDice).GetValue(character);
                return $"{hitDice} HD Monster";

            case Starship:
                var hullClass = characterSystem.GetProperty<GameProperty>(character, HullClass).GetValue(character);
                var cp = characterSystem.GetProperty<GameCounter>(character, CommandPoints).GetValue(character);
                return $"{hullClass} class starship ({cp} CP)";

            default:
                throw new ArgumentException($"Unrecognised character system: {characterSystem.Name}", nameof(character));
        }
    }

    protected override CombatantBase? EncounterNextRound(Encounter encounter, IRandomWrapper random)
    {
        // Nothing special to do here
        return encounter.GetCurrentCombatant();
    }

    private static AttributeBonusCounter BuildAttribute(
        GamePropertyGroupBuilder attributesBuilder,
        GamePropertyGroupBuilder attributeBonusesBuilder,
        string attributeName)
    {
        GameAbilityCounter attributeCounter = new(attributeName);
        AttributeBonusCounter bonusCounter = new(attributeCounter);
        attributesBuilder.AddProperty(attributeCounter);
        attributeBonusesBuilder.AddProperty(bonusCounter);
        return bonusCounter;
    }

    public static GameCounter BuildSkill(GamePropertyGroupBuilder builder, string skillName, AttackBonusCounter attackBonusCounter)
    {
        SkillCounter skillCounter = new(skillName, attackBonusCounter);
        builder.AddProperty(skillCounter);
        return skillCounter;
    }

    private static GameCounterRollResult RollMonsterInitiative(MonsterCombatant combatant,
        Encounter encounter, IRandomWrapper random, int rerolls, ulong userId)
    {
        var matchingMonster = encounter.Combatants.OfType<MonsterCombatant>()
            .FirstOrDefault(c => c.Name == combatant.Name && c.UserId == userId);

        if (matchingMonster is { Initiative: { } roll, InitiativeRollWorking: { } working })
        {
            // We have already rolled for this monster type -- re-use the same one.
            return new GameCounterRollResult
            { CounterName = "Initiative", Error = GameCounterRollError.Success, Roll = roll, Working = working };
        }

        // In SWN, monsters roll 1d8 for initiative (no dexterity bonus)
        ParseTreeBase parseTree = DiceRollParseTree.BuildWithRerolls(8, rerolls, 1);
        var initiative = parseTree.Evaluate(random, out working);

        return new GameCounterRollResult
        {
            CounterName = "Initiative",
            Error = GameCounterRollError.Success,
            Roll = initiative,
            Working = working
        };
    }
}
