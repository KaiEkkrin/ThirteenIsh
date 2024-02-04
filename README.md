# ThirteenIsh

An experimental Discord bot

## Setup

I followed the instructions in [Discord.net -- Your First Bot](https://discordnet.dev/guides/getting_started/first-bot.html) and in [Discord.net -- Getting started with application commands](https://discordnet.dev/guides/int_basics/application-commands/intro.html). The bot will need to be added to servers with the following permissions:

* bot
* applications.commands

And, it will need these bot permissions:

* TODO -- any here?

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

## TO DO

### Character commands re-design

I don't like the sub-command group thing or the individual settings for different abilities. Also, more of the character sheet needs to be customised per game system and I do want to do the basics of both 13th Age and Dragonbane (and whatever else happens to come along.)

* `character-add` -- add a new character -- select a game system from a drop-down, then receive a form to fill in with the non-custom bits.
* `character-edit` -- select a character, get the same form to edit.
* `character-list` -- list all characters
* `character-remove` -- remove a character
* `character-roll` -- rolls based on the selected stat and the character's game system
* `character-show` -- show a character's sheet
* `character-counter-add` or `counter-add` -- add a custom counter with a reset value
* `character-counter-edit` or `counter-edit` -- edit a custom counter with its reset value
* `character-counter-list` or `counter-list` -- list all counters for a character
* `character-counter-remove` or `counter-remove` -- remove a counter from a character

### Player character commands

Should I have `pc-roll` to roll taking into account some variables known by the game system? Other convenience things e.g. attacking and auto-applying variable changes to target(s)? (luxury!)

* `pc-rest` -- applies a level of "rest" to the adventurer's variables taking into account game system and custom counters
* `pc-var` -- alters a counter variable

### Combat commands basic design

* `combat-add` -- add a monster to the combat (game system-specific config...)
* `combat-begin` -- GM command only -- starts combat in the current game system
* `combat-end` -- GM command only -- ends combat in the current game system
* `combat-join` -- adds current adventurer to the combat
* `combat-leave` -- leaves combat with the current adventurer
* `combat-remove` -- remove a monster from the combat
