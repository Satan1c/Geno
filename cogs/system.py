# -*- coding: utf-8 -*-

import json
import re
from datetime import datetime

import psutil

import discord
from bot.bot import Geno
from bot.bot import bot as b
from discord.ext import commands as cmd

url_rx = re.compile(r'https?://(?:www\.)?.+')
checks = b.checks


class System(cmd.Cog):
    def __init__(self, bot: Geno):
        self.bot = bot
        self.config = bot.servers
        self.webhooks = bot.webhooks
        self.commands = bot.cmds
        self.streamers = bot.streamers
        self.profile = bot.profiles
        self.utils = bot.utils
        self.twitch = bot.twitch
        self.models = bot.models
        self.Paginator = bot.Paginator
        self.EmbedGenerator = bot.EmbedGenerator
        self.bot_invite = "https://discord.com/oauth2/authorize?client_id={id}&permissions={perms}&scope=bot"
        self.supp_link = "https://discord.gg/NSkg6N9"
        self.patreon_link = "https://patreon.com/satan1c"
        self.reactions = ('⬅', '⏹', '➡')
        self.urls = ({"Bot invite:": self.bot_invite.format(id=bot.user.id, perms=536210647)},
                     {"Support server:": self.supp_link},
                     {"Patreon:": self.patreon_link},
                     {"SD.C": "https://bots.server-discord.com/648570341974736926"},
                     {"D.Boats": "https://discord.boats/bot/648570341974736926"},
                     {"Top-Bots": "https://top-bots.xyz/bot/648570341974736926"})

    @cmd.command(name="Test", aliases=['test'], hidden=True)
    @cmd.is_owner()
    @cmd.check(checks.is_off)
    async def _test(self, ctx: cmd.Context):
        proc = psutil.Process()
        with proc.oneshot():
            mem = proc.memory_full_info()
            ram = f"uss: {round((mem.uss / 1024) / 1024, 1)}\n" \
                  f"vms: {round((mem.vms / 1024) / 1024, 1)}" \
                  f"rss: {round((mem.rss / 1024) / 1024, 1)}"
            await ctx.send(ram)

    @cmd.command(name="Prefix", aliases=['prefix', 'prf', 'set_prefix', 'set_pref', 'префикс', 'преф'],
                 usage="prefix <prefix>",
                 description="""
    prefix - any prefix what you want
     examples: `-g`, `!`, `some_awesome_prefix`
     default: `g-`
    
    Changing current server prefix, to "prefix"
    :-:
    prefix - любой префикс, который вам надо
     примеры: `-g`, `!`, `some_awesome_prefix`
     по умолчанию: `g-`
    
    Изменяет текущий префикс на сервере, на "prefix"
    """)
    @cmd.check(checks.is_off)
    @cmd.has_guild_permissions(manage_messages=True)
    async def _prefix(self, ctx: cmd.Context, *, prefix: str = "g-"):
        cfg = self.config.find_one({"_id": f"{ctx.guild.id}"})
        raw = cfg['prefix']
        if str(raw) == str(prefix):
            raise cmd.BadArgument("New prefix can't be equals old")

        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"prefix": prefix}})
        await ctx.send(embed=discord.Embed(title="Prefix change",
                                           description=f"From: `{raw}`\nTo: `{prefix}`",
                                           colour=discord.Colour.green(),
                                           timestamp=datetime.now())
                       .set_footer(text=str(ctx.author),
                                   icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256)))

        del cfg

    @cmd.command(name="Twitch", aliases=['twitch', 'твитч'], usage="twitch [channel | \"remove\"] <nickname>",
                 description="""
    channel - can be channel **mention** or **channel id**,
     example: <#648622079779799040>, `648622079779799040`
     default: register in current channel
     
    nick - must be "nickname" or url of twitch.tv streamer,
     examples: `satan1clive`, https://twitch.tv/satan1clive
     default: `satan1clive`
    
    Register "channel", for auto announcement about new streams on twitch.tv channel "nick"
    or
    Remove twitch.tv channel "nick" from 
    
    Remove command exmple:
     -twitch remove satan1clive
     default: `satan1clive`
    :-:
    channel - должен быть **упоминанием** или **id пользователя**,
     пример: <#648622079779799040>, `648622079779799040`
     по умолчанию: регистрирует в текущий канал
     
    nick - должен быть "никнеймом" или ссылкой на twitch.tv стримера,
     примеры: `satan1clive`, https://twitch.tv/satan1clive
     по умолчанию: `satan1clive`
    
    Регистрирует "channel", для автоматических оповещений, о трансляциях на twitch.tv канале "nick"
    """)
    @cmd.check(checks.is_off)
    @cmd.has_guild_permissions(manage_channels=True)
    @cmd.bot_has_guild_permissions(manage_channels=True)
    async def _twitch(self, ctx: cmd.Context, channel: str = None, *, nick: str = None):
        if channel == "remove":
            if not nick:
                nick = "satan1clive"
            em = discord.Embed(title="Twitch remove")
            cfg = self.streamers.find_one({"_id": str(nick)})
            if not cfg:
                em.description = f"`{nick}` was not found in announcements"
                return await ctx.send(embed=em)
            if len(cfg['servers']):
                arr = self.utils.bubble_sort([int(i['id']) for i in cfg['servers']])
                index = self.utils.binary_search(arr, ctx.guild.id)
                arr.pop(index)
                cfg['servers'] = arr
                em.description = f"Announcements from channel: `{nick}`"
            else:
                em.description = f"`{nick}` was not found in announcements"
                return await ctx.send(embed=em)

            self.streamers.update_one({"_id": nick}, {"$set": dict(cfg)})
            return await ctx.send(embed=em)

        nick, channel = await self.utils.twitch(ctx, nick, channel)

        query = self.twitch.get_user_query(nick)
        res2 = self.twitch.get_response(query).json()['data'][0]
        if not res2:
            raise cmd.BadArgument("not found")

        cfg = self.streamers.find_one({"_id": str(nick)})

        if not cfg:
            cfg = self.models.Streamer(nick).get_dict()

            if ctx.guild.id not in [int(i['id']) for i in cfg['servers'] if i and i['id'] and i['channel']]:
                cfg['servers'].append({"id": f"{ctx.guild.id}", "channel": f"{channel.id}"})

            self.streamers.insert_one(cfg)
        else:
            if ctx.guild.id not in [int(i['id']) for i in cfg['servers'] if i and i['id'] and i['channel']]:
                cfg['servers'].append({"id": f"{ctx.guild.id}", "channel": f"{channel.id}"})
            else:
                if len(cfg['servers']):
                    arr = self.utils.bubble_sort([int(i['id']) for i in cfg['servers']])
                    index = self.utils.binary_search(arr, ctx.guild.id)
                    arr[index] = {"id": f"{ctx.guild.id}", "channel": f"{channel.id}"}
                else:
                    cfg['servers'].append({"id": f"{ctx.guild.id}", "channel": f"{channel.id}"})

            self.streamers.update_one({"_id": nick}, {"$set": dict(cfg)})
        r1 = res2['login']
        r2 = res2['display_name']
        r3 = res2['profile_image_url']

        del cfg, res2

        return await channel.send(embed=discord.Embed(title="Streamer announcements add for:",
                                                      description=f"[{r1}](https://twitch.tv/{r2})\n"
                                                                  f"In channel: <#{channel.id}>",
                                                      colour=discord.Colour.green(),
                                                      timestamp=datetime.now())
                                  .set_image(url=f"{r3}"))

    @cmd.command(name="Role Reactions",
                 aliases=['rr', 'role_reactions', 'rolereactions', 'рр', 'роли_по_реакциям', 'ролипореакцим', 'рлпрк'],
                 usage="role_reactions <message id> <list: <emoji id> <role mention or id> >",
                 description="""
    message id - must be integer number of message on what you wanna add reactions
     example: `648622822889095172`
     
    emoji - must be integer number of emoji that you wanna add as reaction
     example: `123456789012345678`
    role - must be id or mention of role that you wanna mark as reaction role
     examples: `648571307860164618`, @everyone
    
    command usage example:
     -role_reaction 648622822889095172 123456789012345678 648571307860164618 098765432109876543 @some_role
     -role_reaction message emoji role_id emoji role_mention
     
    Adds "emojis" to "message", and mark "roles" as reaction roles
    :-:
    message id - должен быть, целым числом сообщения, на которое нужно добавить реакцию
     пример: `648622822889095172`
     
    emoji - должен быть целым чилом, id емодзи которое нужно добавить как реакцию
     пример: `123456789012345678`
    role - должен быть id роли или ее упоминанием
     примеры: `648571307860164618`, @everyone
    
    пример использования команды:
     -role_reaction 648622822889095172 123456789012345678 648571307860164618 098765432109876543 @some_role
     -role_reaction message emoji role_id emoji role_mention
     
    Добавляет "emojis" под "message", и отмечает "roles" как роли по реакциям
    """)
    @cmd.check(checks.is_off)
    @cmd.has_guild_permissions(manage_channels=True, manage_roles=True)
    @cmd.bot_has_guild_permissions(manage_channels=True, manage_roles=True)
    async def _reaction_roles(self, ctx: cmd.Context, message: str, *args):
        key, value, message = await self.utils.reaction_roles(ctx, message, args)

        data = {}
        for i in range(len(key)):
            data[f"{key[i].id}"] = value[i].id
        print(data)
        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"reactions": dict(data)}})
        for i in key:
            print(i)
            await message.add_reaction(i)

        del data

    @cmd.command(name="Disable", aliases=['disable', 'отключить'], usage="disable <category name | command alias>",
                 description="""
    Category name - must be full name of category
     example: `Music`
    
    Command alias - must be one of command aliases
     examples: `p`, `ban`, `srv`
    
    Disable all "category" commands or "command" on server
    :-:
    Category name - должен быть полным названием категории
     пример: `Music`
    
    Command alias - должен быть одним из вариантов использования команды
     примеры: `p`, `ban`, `srv`
    
    Отключает все команды в "category" или команду "command" на сервере
    """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    @cmd.has_guild_permissions(manage_guild=True)
    async def _disable_command_or_category(self, ctx: cmd.Context, *, target: str):
        if not target:
            raise cmd.BadArgument("Give name of category or command alias")
        r = await checks.is_off(ctx)
        print(r)
        target = target.split(" ")
        target = [i.lower() for i in target]

        cfg = self.commands.find_one({"_id": f"{ctx.guild.id}"})

        if not cfg:
            data = self.models.Commands(ctx.guild).get_dict()
            self.commands.insert_one(dict(data))
            cfg = self.commands.find_one({"_id": f"{ctx.guild.id}"})

        cogs = [i for i in self.bot.cogs if i.lower() in target]

        cmds = [i.name for j in self.bot.cogs for i in self.bot.cogs[j].walk_commands()
                if not i.hidden and j not in ["Jishaku", "Enable"] and len([v for v in target if v in i.aliases]) > 0]

        if not cmds and not cogs:
            raise cmd.BadArgument('Nothing found!')

        if cogs:
            if cogs[0] in cfg['cogs']:
                return await ctx.send("This category already disabled")

            cfg['cogs'].append(cogs[0])
            res = f"Category: {cogs[0]}"
        elif cmds:
            if cmds[0] in cfg['commands']:
                return await ctx.send("This command already disabled")

            cfg['commands'].append(cmds[0])
            res = f"Command: {cmds[0]}"

        self.commands.update_one({"_id": f"{ctx.guild.id}"}, {"$set": dict(cfg)})
        await ctx.send(embed=discord.Embed(title="Disable:",
                                           description=res,
                                           colour=discord.Colour.green(),
                                           timestamp=datetime.now()))

        del cfg

    @cmd.command(name="Enable", aliases=['enable', 'включить'], usage="enable <category name | command alias>",
                 description="""
    Category name - must be full name of category
     example: `Music`
    
    Command alias - must be one of command aliases
     examples: `p`, `ban`, `srv`
    
    Enable all "category" commands or "command" on server
    :-:
    Category name - должен быть полным названием категории
     пример: `Music`
    
    Command alias - должен быть одним из вариантов использования команды
     примеры: `p`, `ban`, `srv`
    
    Включает все команды в "category" или команду "command" на сервере
    """)
    @cmd.guild_only()
    @cmd.has_guild_permissions(manage_guild=True)
    async def _enable_command_or_category(self, ctx: cmd.Context, *, target: str):
        if not target:
            raise cmd.BadArgument("Give name of category or command alias")

        target = target.split(" ")
        target = [i.lower() for i in target]

        cfg = self.commands.find_one({"_id": f"{ctx.guild.id}"})

        if not cfg:
            raise cmd.BadArgument("All commands or categories are enabled")

        cogs = [i for i in self.bot.cogs if i.lower() in target]

        cmds = [i.name for j in self.bot.cogs for i in self.bot.cogs[j].walk_commands()
                if not i.hidden and j not in ["Jishaku", "Enable"] and len([v for v in target if v in i.aliases]) > 0]

        if not cmds and not cogs:
            raise cmd.BadArgument('Nothing found!')

        if cogs:
            if cogs[0] not in cfg['cogs']:
                return await ctx.send("This category already enabled")

            index = [i for i in range(len(cfg['cogs'])) if cfg['cogs'][i].lower() == cogs[0].lower()][0]
            res = cfg['cogs'].pop(index)
            res = f"Category: {res}"
        elif cmds:
            if cmds[0] not in cfg['commands']:
                return await ctx.send("This command already enabled")

            index = [i for i in range(len(cfg['commands'])) if cfg['commands'][i].lower() == cmds[0].lower()][0]
            res = cfg['commands'].pop(index)
            res = f"Command: {res}"

        self.commands.update_one({"_id": f"{ctx.guild.id}"}, {"$set": dict(cfg)})
        await ctx.send(embed=discord.Embed(title="Enable:",
                                           description=res,
                                           colour=discord.Colour.green(),
                                           timestamp=datetime.now()))

        del cfg

    @cmd.command(name="Create Webhook", aliases=['sendwh', 'send_webhook'], usage="create_webhook [url] <json | alias>",
                 description="""
    URL - must be full formatted url address to some text-channel webhook
    
    JSON data - JSON format, u can take it on this [site](https://geno.page.link/tobR)
     example: `{ \"embeds\": [ { \"title\": \"Some title\", \"description\": \"Some description\", \"author\": {\"name\": \"Some author name\"}, \"footer\": {\"text\": \"Some footer text\"} } ] }`
    
    Alias - name of saved webhook pattern, if u have some
    
    Send the webhook that was given by `url` and `json data` or pattern `alias`
    :-:
    URL - должен быть полноформатной ссылкой на вебхук любого текстового-канала
    
    JSON data - JSON формат, выможете создать его на этом [сайте](https://geno.page.link/tobR)
     пример: `{ \"embeds\": [ { \"title\": \"Some title\", \"description\": \"Some description\", \"author\": {\"name\": \"Some author name\"}, \"footer\": {\"text\": \"Some footer text\"} } ] }`
    
    Alias - имя под которым был сохранен паттерн вебхука
    
    Отправляет вебхук который задан `url` и `json data` или `alias` паттерна
    """)
    @cmd.check(checks.is_off)
    @cmd.bot_has_guild_permissions(manage_channels=True)
    async def _send_webhook(self, ctx: cmd.Context, url: str = None, *, data: str = None):
        if url_rx.match(url):
            webhook = discord.Webhook.from_url(url=url, adapter=discord.RequestsWebhookAdapter())
            data = json.loads(data)

        else:
            data = self.webhooks.find_one({"_id": f"{ctx.guild.id}"})
            if not data or url not in data['webhooks']:
                raise cmd.BadArgument(f"No saved pattern `{url}` was found")
            else:
                data = data['webhooks'][url]

            webhook = discord.Webhook.from_url(url=data['url'], adapter=discord.RequestsWebhookAdapter())

        if "embeds" in data and len(data['embeds']) > 0:
            ems = []
            for i in data['embeds']:
                ems.append(discord.Embed.from_dict(i))

            data['embeds'] = ems

        webhook.send(content=data['content'] if "content" in data else None,
                     username=data['username'] if "username" in data else webhook.name,
                     avatar_url=data['avatar_url'] if "avatar_url" in data else webhook.avatar_url,
                     embeds=data['embeds'] if "embeds" in data else None)

        del data

    @cmd.command(name="Save Pattern", aliases=['savept', 'save_pattern'], usage="save_pattern <alias> <url> <json>",
                 description="""
    Alias - an alias of new pattern to save it
    
    URL - must be full formatted url address to some text-channel webhook
    
    JSON data - JSON format, u can take it on this [site](https://geno.page.link/tobR)
     example: `{ \"embeds\": [ { \"title\": \"Some title\", \"description\": \"Some description\", \"author\": {\"name\": \"Some author name\"}, \"footer\": {\"text\": \"Some footer text\"} } ] }`
    
    Creates an unique pattern by given `alias` with `url` and `json` for server
    :-:
    Alias - имя нового пвттерна для его сохранения
    
    URL - должен быть полноформатной ссылкой на вебхук любого текстового-канала
    
    JSON data - JSON формат, выможете создать его на этом [сайте](https://geno.page.link/tobR)
     пример: `{ \"embeds\": [ { \"title\": \"Some title\", \"description\": \"Some description\", \"author\": {\"name\": \"Some author name\"}, \"footer\": {\"text\": \"Some footer text\"} } ] }`
    
    Создает уникальный паттерн по указаному `alias` с `url` и `json` для сервера
    """)
    @cmd.check(checks.is_off)
    @cmd.bot_has_guild_permissions(manage_channels=True)
    async def _save_pattern(self, ctx: cmd.Context, alias: str, url: str, *, data: str):
        if not alias or not data or not url:
            raise cmd.BadArgument(
                f"To create webhook pattern give {'json data' if not data else 'alias' if not alias else 'url'}")

        if url_rx.match(url):
            webhook = discord.Webhook.from_url(url=url, adapter=discord.RequestsWebhookAdapter())

        else:
            res = self.webhooks.find_one({"_id": f"{ctx.guild.id}"})
            if not res or alias not in res['webhooks']:
                raise cmd.BadArgument(f"No saved pattern `{url}` was found")
            else:
                url = res['webhooks'][alias]['data']['url']
                data = url + data

            webhook = discord.Webhook.from_url(url=url, adapter=discord.RequestsWebhookAdapter())

        data = json.loads(data)
        res = self.webhooks.find_one({"_id": f"{ctx.guild.id}"})
        if res and alias in res['webhooks']:
            res['webhooks'][alias] = {
                "url": url,
                "content": data['content'] if "content" in data else None,
                "username": data['username'] if "username" in data else webhook.name,
                "avatar_url": data['avatar_url'] if "avatar_url" in data else webhook.avatar_url,
                "embeds": data['embeds'] if "embeds" in data else None
                }

            self.webhooks.update_one({"_id": f"{ctx.guild.id}"}, {
                "$set": {
                    "webhooks": res['webhooks']
                    }
                })
        elif res:
            res['webhooks'][alias] = {
                "url": url,
                "content": data['content'] if "content" in data else None,
                "username": data['username'] if "username" in data else webhook.name,
                "avatar_url": data['avatar_url'] if "avatar_url" in data else webhook.avatar_url,
                "embeds": data['embeds'] if "embeds" in data else None
                }

            self.webhooks.update_one({"_id": f"{ctx.guild.id}"}, {
                "$set": {
                    "webhooks": res['webhooks']
                    }
                })
        elif not res:
            self.webhooks.insert_one({
                "_id": f"{ctx.guild.id}",
                "webhooks": {
                    alias: {
                        "url": url,
                        "content": data['content'] if "content" in data else None,
                        "username": data['username'] if "username" in data else webhook.name,
                        "avatar_url": data['avatar_url'] if "avatar_url" in data else f"{webhook.avatar_url}",
                        "embeds": data['embeds'] if "embeds" in data else None
                        }
                    }
                })

        del data, res

    @cmd.command(name="Delete Pattern", aliases=['delpt', 'delete_pattern'], usage="delete_pattern <alias>",
                 description="""
    Alias - name of saved pattern to delete
    
    Delete `alias` pattern from server 
    :-:
    Alias - мя сохраненного паттерна который будет удален
    
    Удаляет `alias` паттерн с сервера
    """)
    @cmd.check(checks.is_off)
    @cmd.bot_has_guild_permissions(manage_channels=True)
    async def _delete_pattern(self, ctx: cmd.Context, alias: str):
        if not alias:
            raise cmd.BadArgument("To delete pattern give alias")

        data = self.webhooks.find_one({"_id": f"{ctx.guild.id}"})

        if not data or alias not in data['webhooks']:
            raise cmd.BadArgument(f"Not found {'saved patterns' if not data else alias + ' pattern'}")

        data['webhooks'].pop(alias)
        self.webhooks.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"webhooks": data['webhooks']}})

        del data


def setup(bot):
    bot.add_cog(System(bot))
