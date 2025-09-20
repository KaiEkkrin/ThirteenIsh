# Stars Without Number Support

## Overview

ThirteenIsh includes support for the **Stars Without Number: Revised Edition** tabletop RPG system. This is a sci-fi OSR game that combines classic D&D-style mechanics with modern design elements for space adventure campaigns.

**Design Philosophy**: ThirteenIsh focuses on enabling gameplay with pre-created characters rather than character creation. Players are expected to create characters using external tools and import/manually configure them in ThirteenIsh for rolling and encounter tracking.

## Current Implementation Status

### ✅ Implemented Features

**Core Character System:**
- [x] Six attributes (Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma)
- [x] Attribute bonus calculations (-2 to +2 based on 3-18 range)
- [x] Character classes (Expert, Psychic, Warrior) with dual-class support
- [x] Level tracking (starts at 1)

**Skills System:**
- [x] All 19 core skills (Administer, Connect, Exert, Fix, Heal, Know, Lead, Notice, Perform, Pilot, Program, Punch, Shoot, Sneak, Stab, Survive, Talk, Trade, Work)
- [x] All 6 psychic skills (Biopsionics, Metapsionics, Precognition, Telekinesis, Telepathy, Teleportation)
- [x] Skill rolling (2d6 + skill bonus + attribute bonus)
- [x] Unskilled penalties (-1 skill level)

**Derived Statistics:**
- [x] Armor Class (10 + armor value + Dex bonus)
- [x] Attack Bonus (class and level dependent)
- [x] Hit Points (class-based with Con bonus, house rule for consistent HP)
- [x] Saving Throws (Physical, Mental, Evasion - calculated as 15 minus best relevant attribute bonus)
- [x] Effort (for psychic characters, based on class and psychic skills)

**Equipment:**
- [x] Armor Value tracking (base 10, modifiable for different armor types)

**Combat:**
- [x] Attack rolls (1d20 + skill bonus + attribute bonus + attack bonus)
- [x] Combat damage (via `combat damage` command with damage dice and counters)

**Character Customization:**
- [x] Custom counters (behave as custom skills with roll functionality)

### ❌ Missing Features

## NPC/Monster Support (HIGH PRIORITY)

- [ ] **Monster System** (Rulebook p195+)
  - [ ] `EncounterAdd` implementation for adding monsters to encounters
  - [ ] Monster character system and stat blocks
  - [ ] Monster-specific encounter rules and behavior

- [ ] **Creature Types**
  - [ ] **Humanoid Monsters** (p195)
    - [ ] Basic humanoid stat blocks
    - [ ] Equipment and skill variations
    - [ ] Social/faction mechanics
  - [ ] **VI (Artificial Intelligence) Monsters** (p195+)
    - [ ] Virtual Intelligence stat blocks
    - [ ] Digital combat mechanics
    - [ ] Network and system interaction rules
  - [ ] **Beast Monsters** (p195+)
    - [ ] Animal and alien creature stat blocks
    - [ ] Natural weapons and abilities
    - [ ] Environmental and territorial behavior

- [ ] **Vehicle Combat**
  - [ ] **Vehicles in Combat**
    - [ ] Vehicle stat blocks and properties
    - [ ] Vehicle-to-vehicle combat mechanics
    - [ ] Crew positions and actions
    - [ ] Damage and repair systems
  - [ ] **Starships**
    - [ ] Starship combat system
    - [ ] Ship modules and components
    - [ ] Space combat positioning and movement
    - [ ] Boarding actions and ship-to-ship combat

- [ ] **Mech Combat**
  - [ ] Mech stat blocks and customization
  - [ ] Mech combat mechanics and special rules
  - [ ] Pilot integration and ejection systems
  - [ ] Mech equipment and weapon systems

## Enhanced Combat Systems (MEDIUM PRIORITY)

- [ ] **Combat Actions**
  - [x] Basic attack rolls (1d20 + skill + attribute + attack bonus)
  - [x] Damage application (via `combat damage` command with dice and modifiers)
  - [ ] Action economy (Move, Main, Instant, On Turn actions)
  - [ ] Snap attacks (-4 penalty, consumes main action)
  - [ ] Execution attacks (skill check vs difficulty instead of to-hit)
  - [ ] Two-weapon fighting rules
  - [ ] Ranged vs melee interaction rules

- [ ] **Death & Dying**
  - [ ] 0 HP = 6 rounds to live (damage application works, death rules TODO)
  - [ ] Stabilization mechanics (Int/Heal or Dex/Heal as Main Action)
  - [ ] Mortal wound system

## Character Creation & Advancement (LOW PRIORITY - External Tools Preferred)

**Note**: These features are deprioritized as players should use external character builders and manually configure characters in ThirteenIsh.

- [ ] **Character Foci**
  - [ ] Foci abilities and special rules (especially Specialist focus)
  - [ ] Skill bonuses and special abilities from foci

  **Current Workaround**: Players can manually specify rerolls equal to their Specialist level for that skill.
  See TODO in ThirteenIsh.Game.Swn.SkillCounter.Roll

- [ ] **Character Backgrounds**
  - [ ] Background-specific starting packages and descriptions

- [ ] **Character Advancement**
  - [ ] Experience points and level-up mechanics
  - [ ] Automatic saving throw improvements
  - [ ] Skill point allocation

- [ ] **Psychic Techniques**
  - [ ] Level-0 abilities for each psychic discipline
  - [ ] Higher-level psychic techniques (levels 1-4)
  - [ ] Effort expenditure and recovery mechanics

### Monster Support Details

"Stars Without Number" leaves much of the details of monsters up to the Game Master, unlike more rules-heavy games like Starfinder. There are some key stats for "monsters" (hostile NPCs) on page 195 of the source book. Monsters need to have the following attributes, all of which must be directly settable by the GM rather than computed:

- [ ] Hit Dice (HD)
- [ ] Armor Class (AC)
- [ ] Attack Bonus (AB)
- [ ] Damage Bonus
- [ ] Move
- [ ] Morale (ML)
- [ ] Skills -- the same skill set as for PCs
- [ ] Saves -- the same set of saves as for PCs.

**Morale** is an attribute that monsters have and PCs do not. When a monster faces "unusual peril" (as deemed by the GM) they must make a **Morale check**. For this they roll 2d6; if they roll less than or equal to their Morale score they continue fighting, if they roll greater than their Morale score, they attempt to escape the encounter. We can implement this quite easily with a custom MoraleCounter class with its own implementation of Roll.

## Currently Throwing NotImplementedException

**SwnSystem.cs:**
- [ ] `EncounterAdd` (line 164)
- [x] `EncounterBegin` (line 169) - ✅ **COMPLETED**
- [x] `EncounterJoin` (line 175) - ✅ **COMPLETED**
- [x] `GetCharacterSummary` (line 180) - ✅ **COMPLETED**
- [x] `EncounterNextRound` (line 185) - ✅ **COMPLETED**

**SwnCharacterSystem.cs:**
- [x] `BuildCustomCounter` (line 27) - ✅ **COMPLETED**

## Implementation Notes

### House Rules Currently Applied
- **Hit Points**: Instead of rolling, base HP is 6 at level 1, plus 3.5 (rounded down) per additional level, plus (class bonus + Con bonus) × level
- **Classes**: Support for dual-classing (full Expert/Psychic, partial Warrior, etc.)

### Architecture Notes
- Game system follows the standard ThirteenIsh pattern with `GameSystem` and `CharacterSystem` base classes
- Counters use dependency injection for calculated values (e.g., AC depends on armor value and Dex bonus)
- Skills inherit from `SkillCounter` with SWN-specific rolling mechanics
- Saving throws are calculated counters based on attribute bonuses

## Development Priority

**High Priority** (Core gameplay with pre-created characters):
1. **Monster System** - `EncounterAdd` implementation and monster stat blocks
2. **Creature Types** - Humanoid, VI, and Beast monsters (Rulebook p195+)
3. **Vehicle Combat** - Basic vehicle and starship combat mechanics

**Medium Priority** (Enhanced combat):
1. **Mech Combat** - Mech stat blocks and combat rules
2. **Advanced Combat Actions** - Action economy, snap attacks, execution attacks
3. **Death & Dying** - Stabilization and mortal wound mechanics

**Low Priority** (Character creation - use external tools):
1. Character foci system (Specialist focus workaround available)
2. Character backgrounds and starting packages
3. Experience points and advancement
4. Psychic techniques