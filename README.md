# ThirteenIsh

An experimental Discord bot

## Setup

I followed the instructions in [Discord.net -- Your First Bot](https://discordnet.dev/guides/getting_started/first-bot.html) and in [Discord.net -- Getting started with application commands](https://discordnet.dev/guides/int_basics/application-commands/intro.html). The bot will need to be added to servers with the following permissions:

* bot
* applications.commands

And, it will need these bot permissions:

* Manage Messages (8192)

## Database

ThirteenIsh uses MongoDB. You can run up a basic Docker container:

```powershell
docker run --memory=4g --name thirteenish-mongo --restart unless-stopped -d -p 27017:27017 -v thirteenish-mongo-data:/data/db mongo:5
```

If you're using that, you can use the connection string `mongodb://localhost:27017`

## Configuration settings

Configure these via command line, environment variables or user secrets.

* "BotToken": the Discord bot token.
* "MongoConnectionString": the MongoDB connection string.

## Docker-compose based deployment

* Create a file "my.docker.env" in the project root and add to it `BotToken="...my bot token..."`.
* Run e.g. `docker-compose up -d --build`

## TO DO

(Lots of the below is out of date compared to how I actually did it and I should update when things are more complete.)

### Adventure re-design

Not much here. When creating an adventure, select a game system from the drop-down. Game system cannot be changed after creation (all hell would break loose.)

### Character re-design

The CharacterSheet is now a base class for (Game System)CharacterSheet. (So necessarily each character is tied to a game system, can only be added to adventures in that system etc.)

A **Property** is a property of a character's sheet. It has a name and an enumerated value. E.g. character class.

A **Counter** is a property of a character's sheet. It has a name, an optional alias, a numeric value, a minimum value, a maximum value, whether or not it has an associated variable, and if it does, an optional rest interval and an optional rest amount.

Characters have fixed counters (automatically added by the character sheet and filled in by the user on creation), derived counters (automatically added by the character sheet with values computed from the fixed counters etc), and custom counters (added and removed at any time by the user, can contain anything.)

Rest intervals are an enumerated value whose possible selections depend on the game system.

### Character commands re-design

I don't like the sub-command group thing or the individual settings for different abilities. Also, more of the character sheet needs to be customised per game system and I do want to do the basics of both 13th Age and Dragonbane (and whatever else happens to come along.)

* `character-add` -- add a new character -- select a game system from a drop-down, then receive a form to fill in with the non-custom bits.
* `character-edit` -- select a character, get the same form to edit.
* `character-get` -- show a character's sheet
* `character-list` -- list all characters
* `character-remove` -- remove a character
* `character-roll` -- rolls based on the selected stat and the character's game system
* `character-set` -- set a specific property to a specific value (avoids the form)
* `character-counter-add` or `counter-add` -- add a custom counter with a reset value
* `character-counter-list` or `counter-list` -- list all counters for a character
* `character-counter-remove` or `counter-remove` -- remove a counter from a character
* `character-counter-set` or `counter-set` -- edit a custom counter with its reset value

For now I don't want to try to provide commands to help players level up, track/count bonuses on level up and other fixed things like that -- e.g. ability score boosts, icon relationships, skills in 13th Age. Too messy, and not helpful with character variable tracking across combat and adventures, which is the real point of this bot.

### Player character commands

Should I have `pc-roll` to roll taking into account some variables known by the game system? Other convenience things e.g. attacking and auto-applying variable changes to target(s)? (luxury!) -- TO DO. For Dragonbane, having this roll against a skill would be really useful.

* `pc-rest` -- applies a level of "rest" to the adventurer's variables taking into account game system and custom counters
* `pc-var` -- alters a counter variable

A **Variable** is a property of the adventurer not the character sheet. It corresponds to a counter and tracks the value over time, between the counter minimum and its actual value.

Some game systems may have extra variables, e.g. in Dragonbane, in combat the initiative card variable (corresponds to whether the character has used their initiative yet in this round); in many systems, the death save mechanic; etc.

TO DO: convenience commands in adventure (or in combat when in one), such as attack another character/monster, deal damage to them, etc?

### Combat commands basic design

* `combat-add` -- add a monster to the combat (game system-specific config...)
* `combat-begin` -- GM command only -- starts combat in the current game system
* `combat-end` -- GM command only -- ends combat in the current game system
* `combat-join` -- adds current adventurer to the combat
* `combat-leave` -- leaves combat with the current adventurer
* `combat-remove` -- remove a monster from the combat

### Attacks and damage

* `pc attack` -- attack the target (name of target, name of system-dependent attack ability). Rolls and determines success based on the attack ability and the target.
* `pc damage` -- damage the target (name of target, system-dependent name of counter to damage with suitable default e.g. Hit Points). Rolls and offers a dialog to the target's player letting them accept or deny the damage, if accepted applies it to their variables.

### 13th Age

#### Properties (dropdown select)

These are configured in the character sheet on add or edit.

* Class (list of classes, see page 30.)
* Armor (list of armor categories: None / Light / Heavy.)

Some selections of the class property may add extra fixed counters, e.g. battle cries and spells for the bard (TODO do later after doing the basics.)

#### Fixed counters

These are configured in the character sheet on add or edit.

* Level
* Strength \[STR\]
* Dexterity \[DEX\]
* Constitution \[CON\]
* Intelligence \[INT\]
* Wisdom \[WIS\]
* Charisma \[CHA\]

#### Derived counters

These are automatically created by the character sub-class for the rule system. See page 31 of the rule book.

Should I allow for ad hoc modifier counters for all of these? To account for magic items, feats etc. I could also present that easily in the character sheet add/edit.

* HitPoints \[HP\]
* ArmorClass \[AC\]
* PhysicalDefense \[PD\]
* MentalDefense \[MD\]
* Initiative \[INIT\]
* Recoveries
* RecoveryDie

### Dragonbane

#### Properties

* Kin (needed, because it affects e.g. movement. See page. 9.)
* Profession (like "class" in 13th Age, D&D, see page 15.)

#### Fixed counters

* Strength \[STR\]
* Constitution \[CON\]
* Agility \[AGI\]
* Intelligence \[INT\]
* Willpower \[WIL\]
* Charisma \[CHA\]
* Points for all 30 core skills (see page 30 and onwards.)
* Points for the magical secondary skills.
* ArmorRating (configured as a single number rather than selecting armor types, which would be messy.)

#### Derived counters

* Movement (page 27.)
* HitPoints \[HP\]
* WillpowerPoints \[WP\]

I think it's reasonable to make players make their own custom counters to track weapon durability to begin with...

TO DO come up with something that will let the GM ensure fair skill advancement between player characters (?)

#### Extra variables

* Initiative
* DeathSuccesses (see page 50)
* DeathFails
* Exhausted, Sickly, Dazed, Angry, Scared, Disheartened (these are 0 or 1 only and default to 0)
* Advancement Marks for every skill (0 or 1, see page 29)

#### Rests (see page 54)

* Round: recover 1d6 WP
* Stretch: recover 1d6 HP, 1d6 WP, heal a chosen condition (needs UI...)
* Shift: recover all HP, all WP, lose all conditions
