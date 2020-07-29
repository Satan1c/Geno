# -*- coding: utf-8 -*-
import json
import platform
import re
from datetime import datetime

import psutil

import discord
from discord.ext import commands as cmd

url_rx = re.compile(r'https?://(?:www\.)?.+')


class System(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.config = bot.servers
        self.streamers = bot.streamers
        self.profile = bot.profiles
        self.utils = bot.utils
        self.twitch = bot.twitch
        self.models = bot.models
        self.Paginator = bot.Paginator
        self.EmbedGenerator = bot.EmbedGenerator
        self.arrowl = "<a:31:637653092749410304>"
        self.arrowr = "<a:30:637653060726030337>"
        self.bot_invite = "https://discord.com/oauth2/authorize?client_id={id}&permissions={perms}&scope=bot"
        self.supp_link = "https://discord.gg/NSkg6N9"
        self.patreon_link = "https://patreon.com/satan1c"
        self.reactions = ('⬅', '⏹', '➡')

    @cmd.command(name="Test", aliases=['test'], hidden=True)
    @cmd.is_owner()
    async def _test(self, ctx: cmd.Context, message: str):
        for i in ctx.guild.text_channels:
            try:
                msg = await i.fetch_message(int(re.sub(r"[^0-9]", r"", f"{message}")))
                print(repr(msg))
            except:
                continue

    @cmd.command(name="Announcer", aliases=['announce'], hidden=True)
    @cmd.is_owner()
    async def _announce(self, ctx: cmd.Context, *, text: str):
        print(text)
        owners = [j for i in self.bot.guilds for j in i.members if i.owner_id == j.id]
        for member in owners:
            await member.send(embed=discord.Embed.from_dict(dict(text)))

    @cmd.command(name="Help", hidden=True, aliases=['h', 'help', 'commands', 'cmds'])
    async def _help(self, ctx: cmd.Context, *, command: str = None):
        reg = str(ctx.guild.region if ctx.guild else "en")
        if command:
            cmds = [i for j in self.bot.cogs for i in self.bot.cogs[j].walk_commands()
                    if not i.hidden and j != "Jishaku" and command.lower() in i.aliases]
            if len(cmds) == 0:
                raise cmd.BadArgument('Nothing found!')

            command = cmds[0]
            desc = command.description
            m = await ctx.send(embed=discord.Embed(title=f"{command.name} help:",
                                                   description=f"""
<> - {"required params" if reg != "russia" else "обязательные параметры"}, [] - {"other params" if reg != "russia" else "другие параметры"}
{"Command usage:" if reg != "russia" else "Использование команды:"} {command.usage}
{"Command aliases:" if reg != "russia" else "Иные виды использования:"} `{", ".join(command.aliases)}`

{desc.split(":-:")[0 if reg != "russia" else 1]}
""",
                                                   colour=discord.Colour.green()))
            return await m.delete(delay=120)

        prefix = "-" if not ctx.guild else self.config.find_one({"_id": f"{ctx.guild.id}"})['prefix']
        em = discord.Embed(colour=discord.Colour.green(),
                           title=f'{self.arrowl} Commands list {self.arrowr}',
                           description=f"""prefix: `{prefix}`

                           react {self.reactions[0]} to go next page
                           react {self.reactions[1]} to close \"help\" tab
                           react {self.reactions[2]} to go previous page""")
        embeds = []

        for cog in self.bot.cogs:
            if cog == "Jishaku":
                continue
            listt = list(self.bot.cogs[cog].walk_commands())
            hided = [i.name for i in listt if i.hidden]
            cmds = [f"`{(x + 1) - (len(hided))}`. {listt[x].usage}" for x in range(len(listt)) if not listt[x].hidden]

            if len(cmds) == 0:
                continue

            cmds = "\n".join(cmds)
            embeds.append(discord.Embed(colour=discord.Colour.green(),
                                        title=f'{self.arrowl} {"Commands list" if reg != "russia" else "Список команд"} {self.arrowr}',
                                        description=f"prefix: `{prefix}`"
                                                    f"\nhelp `[{'command alias' if reg != 'russia' else 'использование команды'}]` - {'for single command help' if reg != 'russia' else 'для помощи по одной команде'}")
                          .add_field(name=f"{cog}", value=cmds))
        p = self.Paginator(ctx, embeds=embeds, begin=em)
        await p.start()

    @cmd.command(name="Profile", aliases=['profile', 'prf'], usage="profile", description="""
    :-:
    """, hidden=True)
    @cmd.is_owner()
    async def _profile(self, ctx: cmd.Context):
        prf = self.profile.find_one({"sid": f"{ctx.guild.id}", "uid": f"{ctx.author.id}"})
        em = discord.Embed(title=f"{self.arrowl} {ctx.author.display_name} profile {self.arrowr}",
                           colour=discord.Colour.green(),
                           timestamp=datetime.now())

        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))
        em.add_field(name="messages:", value=f"`{prf['messages']}`")

        return await ctx.send(embed=em)

    @cmd.command(name="Server", aliases=['server', 'srv', 'information', 'info'], usage="server", description="""
    Shows short info about server
    :-:
    Показывает краткую информацию о сервере
    """)
    @cmd.guild_only()
    async def _server(self, ctx: cmd.Context):
        srv = self.config.find_one({"_id": f"{ctx.guild.id}"})
        g = ctx.guild

        em = discord.Embed(title=f"{self.arrowl} {g.name} {self.arrowr}",
                           description=f"prefix: `{srv['prefix']}`",
                           colour=discord.Colour.green(),
                           timestamp=datetime.now())
        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))
        em.add_field(name=f"Members({len(g.members)}{'/' + g.max_members if g.max_members else ''}):",
                     value=f"<:people:730688969158819900> People: `{len([i.id for i in g.members if not i.bot])}`\n"
                           f"<:bot:730688278566535229> Bots: `{len([i.id for i in g.members if i.bot])}`")
        em.add_field(name=f"Channels({len([i.id for i in g.channels if not isinstance(i, discord.CategoryChannel)])}):",
                     value=f"<:voice:730689231139241984> Voices: `{len(g.voice_channels)}`\n"
                           f"<:text:730689530461552710> Texts: `{len(g.text_channels)}`")

        await ctx.send(embed=em)

    @cmd.command(name="Bot", aliases=['bot', 'about'], usage="bot", description="""
    Shows some info about me
    :-:
    Показывает некотороую информацию про меня
    """)
    async def _bot(self, ctx: cmd.Context):
        system = platform.uname()
        cpu = f"`cores: {psutil.cpu_count(logical=True)}" \
              f"\nfrequency: {round(psutil.cpu_freq().current / 1000, 1)}ghz" \
              f"\nusage: {psutil.cpu_percent()}%`"
        ram = f"`volume: {(psutil.virtual_memory().available // 1024) // 1000}mb /" \
              f" {(psutil.virtual_memory().total // 1024) // 1000}mb\n" \
              f"percentage: {round(psutil.virtual_memory().available * 100 / psutil.virtual_memory().total, 1)}%`"

        em = self.EmbedGenerator(target="bot", ctx=ctx, system=system, cpu=cpu, ram=ram, platform=platform,
                                 data=self).get()
        await ctx.send(embed=em)

    @cmd.command(name="Twitch", aliases=['twitch'], usage="twitch [channel | \"remove\"] <nickname>", description="""
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
    @cmd.bot_has_guild_permissions(manage_channels=True)
    @cmd.has_guild_permissions(manage_channels=True)
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

        if not nick and channel:
            nick = channel
            channel = ctx.channel
        elif not channel and not nick:
            channel = ctx.channel
            nick = "satan1clive"
        elif not channel and nick:
            channel = ctx.channel
        elif not nick:
            nick = "satan1clive"
        elif not channel:
            channel = ctx.channel
        if nick and url_rx.match(nick):
            nick = nick.split("/")[-1]
        if len(ctx.message.channel_mentions) > 0:
            channel = ctx.guild.get_channel(int(ctx.message.channel_mentions[0].id))
        elif len(re.sub(r"[^0-9]", r"", f"{channel}")) == 18:
            channel = ctx.guild.get_channel(int(re.sub(r"[^0-9]", r"", channel)))

        query = self.twitch.get_user_query(nick)
        res2 = self.twitch.get_response(query).json()['data'][0]
        if not res2:
            raise cmd.BadArgument("not found")

        cfg = self.streamers.find_one({"_id": str(nick)})

        if not cfg:
            cfg = self.models.Streamer(nick, ctx.guild).get_dict()

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

        return await ctx.send(embed=discord.Embed(title="Streamer announcements add for:",
                                                  description=f"[{res2['display_name']}](https://twitch.tv/{res2['login']})\n"
                                                              f"In channel: <#{channel.id}>",
                                                  colour=discord.Colour.green(),
                                                  timestamp=datetime.now())
                              .set_thumbnail(url=f"{res2['profile_image_url']}"))

    @cmd.command(name="Role Reactions", aliases=['rr', 'role_reactions', 'rolereactions'],
                 usage="role_reactions <message id> <list: <emoji id> <role mention or id> >", description="""
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
    @cmd.bot_has_guild_permissions(manage_channels=True, manage_roles=True)
    @cmd.has_guild_permissions(manage_channels=True, manage_roles=True)
    async def _reaction_roles(self, ctx: cmd.Context, message: str, *args):
        if len(re.sub(r"[^0-9]", r"", f"{message}")) == 18:
            for i in ctx.guild.text_channels:
                try:
                    message = await i.fetch_message(int(re.sub(r"[^0-9]", r"", f"{message}")))
                except:
                    continue

        key = args[::2]
        k = []
        for i in key:
            ids = re.sub(r"[^0-9]", r"", f"{i}")
            if len(ids) == 18:
                e = self.bot.get_emoji(int(ids))
                if not e:
                    k.append(i)
                    continue
                k.append(e)
        key = k

        value = args[1::2]
        print(value)
        if len(ctx.message.role_mentions) > 0 and len(ctx.message.role_mentions) == len(value):
            print("1")
            value = ctx.message.roles_mentions
        else:
            print("2")
            v = []
            m = 0
            for i in range(len(value)):
                print(i)
                if len(re.sub(r"[^0-9]", r"", f"{value[i]}")) == 18:
                    r = ctx.guild.get_role(int(re.sub(r"[^0-9]", r"", f"{value[i]}")))
                    if not r:
                        raise cmd.BadArgument("Role not found")
                    v.append(r)
                elif len(ctx.message.role_mentions) > 0:
                    r = ctx.guild.get_role(ctx.message.role_mentions[m].id)
                    if not r:
                        raise cmd.BadArgument("Role not found")
                    m += 1
                    v.append(r)
            else:
                value = v
            print(value)

        data = {}
        for i in range(len(key)):
            data[f"{key[i].id}"] = value[i].id
        print(data)
        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"reactions": dict(data)}})
        for i in key:
            print(i)
            await message.add_reaction(i)


def setup(bot):
    bot.add_cog(System(bot))
