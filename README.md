![Epic Avatar](https://i.imgur.com/VjtwfMQ.png)
# MopBot
A multi-use C# Discord.NET bot that totally mops!

# Getting Started
* If you don't want to self-host, you can add the bot to your server via [this link](https://discordapp.com/oauth2/authorize?&client_id=386525023390662656&scope=bot&permissions=0).
* Once the bot is there, use `!help` to see a list of available commands, and `!help <group>` to see commands in a command group.
* By default, only a few of its systems are enabled for your server. Use `!systems list` and `!systems enable/disable <system>` to choose the systems you want.

# Systems
This bot has plenty of different systems, which you can mix and match to your liking.

**AutoModerationSystem**

Automatically does some limited moderation through various configurable message filtering.

**ChangelogSystem**

Helps maintaining changelogs, which then can be converted to text lists in different formats: Discord, BBCode, Patreon, etc.

**ChannelLinkingSystem**

A really cool system that lets admins of servers interconnect selected channels of their servers through the bot.

**CommandShopSystem**

Lets users trade **CurrencySystem**
 currencies for pre-defined sudo command executions in custom shops. Commands will be executed as the user who created the shop item that's 'bought'.

**CurrencySystem**

Implements customizable currencies, which people can be rewarded with through other systems, and which people can for example use in CommandShops.

**CustomRoleSystem**

Lets 'VIP' users make unique roles for themselves.

**ImagingSystem**

Currently only contains an avatar-getting command.

**IssueSystem**

In-discord bug tracker. Can write to **ChangelogSystem**
 when an issue gets fixed.

**LoggingSystem**

Allows setting up logging of things like kicks, bans, role changes, message edits & deletes, etc.

**MessageManagementSystem**

Contains commands for sending messages, as well as moving and copying existing messages. Useful!

**ModerationSystem**

This system adds simple moderation commands, like kick, ban and clear.

**PostingSystem**

Commands to simplify large multi-message posts. [[Split]] in input marks a message split.

**RoleSystem**

Contains commands for giving, taking, and mentioning roles. It's also possible to let users join selected roles on their own, with per-role permissions.

**ShowcaseSystem**

Lets admins setup channels with reaction-based voting. There's also spotlighting support, which moves channels with X score to a selected channel, and also gives author customizable rewards if needed.

**TagSystem**

Lets people create and use cross-server tags (aka message shortcuts). Supports tag groups, tag/tag group subscription and globalization. Is cool.

**TriviaSystem**

Lets admins setup trivia channels, in which the bot will ask users questions and give customizable rewards to the first answerers.

**WelcomeSystem**

Welcomes users onto the server with customizable messages.

**XPSystem**

A small XP/leveling system.
