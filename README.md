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

