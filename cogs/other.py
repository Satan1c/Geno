# -*- coding: utf-8 -*-

import platform
import re
from datetime import datetime

import psutil

import discord
from bot.bot import bot as b
from discord.ext import commands as cmd

url_rx = re.compile(r'https?://(?:www\.)?.+')
checks = b.checks


class Other(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.config = bot.servers
        self.utils = bot.utils
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

    @cmd.command(name="Help", hidden=True, aliases=['h', 'commands', 'cmds', 'хелп', 'команды', 'кмд'])
    @cmd.check(checks.is_off)
    async def help(self, ctx: cmd.Context, *, command: str = None):
        reg = str(ctx.guild.region if ctx.guild else "en")
        if command:
            cmds = [i for j in self.bot.cogs for i in self.bot.cogs[j].walk_commands()
                    if not i.hidden and command.lower() in i.aliases]
            if len(cmds) == 0:
                raise cmd.BadArgument('Nothing found!')

            command = cmds[0]
            desc = command.description
            await ctx.send(embed=discord.Embed(title=f"{command.name} help:",
                                               description=f"""
                                                <> - {"required params" if reg != "russia" else "обязательные параметры"}, [] - {"other params" if reg != "russia" else "другие параметры"}
                            
                                                {"Command usage:" if reg != "russia" else "Использование команды:"} {command.usage}
                                                {"Command aliases:" if reg != "russia" else "Иные виды использования:"} `{", ".join(command.aliases)}`

                                                {desc.split(":-:")[0 if reg != "russia" else 1]}""",

                                               colour=discord.Colour.green()))
            return

        prefix = self.bot.prefix if not ctx.guild else await self.config.find_one({"_id": f"{ctx.guild.id}"})
        if not isinstance(prefix, str):
            prefix = prefix['prefix']

        em = discord.Embed(colour=discord.Colour.green(),
                           title=f'Commands list',
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
                                        title=f' {"Commands list" if reg != "russia" else "Список команд"}',
                                        description=f"prefix: `{prefix}`"
                                                    f"\nhelp `["
                                                    f"{'command alias' if reg != 'russia' else 'использование команды'}"
                                                    f"]` - "
                                                    f"{'for single command help' if reg != 'russia' else 'для помощи по одной команде'}")
                          .add_field(name=f"{cog}", value=cmds))
        p = self.Paginator(ctx, embeds=embeds, begin=em)
        await p.start()

    @cmd.command(name="Server", aliases=['srv', 'information', 'info', 'сервер', 'инфо', 'информация'],
                 usage="server", description="""
    Shows short info about server
    :-:
    Показывает краткую информацию о сервере
    """)
    @cmd.guild_only()
    @cmd.check(checks.is_off)
    async def server(self, ctx: cmd.Context):
        srv = await self.config.find_one({"_id": f"{ctx.guild.id}"})
        g = ctx.guild

        em = discord.Embed(title=f"{g.name}",
                           description=f"prefix: `{srv['prefix']}`",
                           colour=discord.Colour.green(),
                           timestamp=datetime.now())
        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))
        em.add_field(name=f"Members({len(g.members)}{'/' + str(g.max_members) if g.max_members else ''}):",
                     value=f"<:people:730688969158819900> People: `{len([i.id for i in g.members if not i.bot])}`\n"
                           f"<:bot:730688278566535229> Bots: `{len([i.id for i in g.members if i.bot])}`\n"
                           f"online: `{len([i.id for i in g.members if not i.bot and i.status is discord.Status.online])}`\n"
                           f"dnd: `{len([i.id for i in g.members if not i.bot and i.status is discord.Status.dnd])}`\n"
                           f"idle: `{len([i.id for i in g.members if not i.bot and i.status is discord.Status.idle])}`\n"
                           f"offline: `{len([i.id for i in g.members if not i.bot and i.status is discord.Status.offline])}`\n")
        em.add_field(name=f"Channels({len([i.id for i in g.channels if not isinstance(i, discord.CategoryChannel)])}):",
                     value=f"<:voice:730689231139241984> Voices: `{len(g.voice_channels)}`\n"
                           f"<:text:730689530461552710> Texts: `{len(g.text_channels)}`")

        await ctx.send(embed=em)

        del srv

    @cmd.command(name="Bot", aliases=['about', 'бот'], usage="bot", description="""
    Shows some info about me
    :-:
    Показывает некотороую информацию про меня
    """)
    @cmd.check(checks.is_off)
    async def info_bot(self, ctx: cmd.Context):
        system = platform.uname()
        proc = psutil.Process()

        with proc.oneshot():
            cpu = f"`cores: {psutil.cpu_count()}" \
                  f"\nfrequency: {round(psutil.cpu_freq().current / 1000, 1)}ghz" \
                  f"\nusage: {psutil.cpu_percent()}%`"

            mem = proc.memory_full_info()
            ram = f"`usage volume: {round((mem.vms // 1024) / 1024, 1)}mb`"

            em = self.EmbedGenerator(target="bot", ctx=ctx, system=system, cpu=cpu, ram=ram, platform=platform,
                                     data=self).get()
            await ctx.send(embed=em)

    @cmd.command(name="Links", aliases=['urls', 'bot_links'], usage="links", description="""
    Shows connected to bot links, like: bot invite, support server, monitors
    :-:
    Показывает ссылки связанные с ботом, по типу: приглашение бота, сервер поддержки, смониторинги
    """)
    @cmd.check(checks.is_off)
    async def info_bot_urls(self, ctx: cmd.Context):
        em = discord.Embed(title=f"{ctx.me.name} {self.bot.version}  urls",
                           colour=discord.Colour.green(),
                           timestamp=datetime.now())
        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

        titles = [k for i in self.urls for k, v in i.items()]
        urls = [v for i in self.urls for k, v in i.items()]

        for i in range(len(self.urls)):
            em.add_field(name=titles[i], value=f"[Click]({urls[i]})")

        await ctx.send(embed=em)

    # @cmd.command(name="Profile", aliases=['профиль', 'profile'], usage="profile")
    # @cmd.check(checks.is_off)
    # async def profile_command(self, ctx: cmd.Context):
    #     activity = f"{str(ctx.author.activities[1].type).split('.')[1]} {ctx.author.activities[1].name}" if "emoji" in dir(ctx.author.activities[0]) else f"{ctx.author.activities[0].emoji} {ctx.author.activities[0].name}"
    #     custom = f"{ctx.author.activities[0].emoji} {ctx.author.activities[0].name}\n" if "emoji" in dir(ctx.author.activities[0]) else None
    #
    #     em = discord.Embed(title=f"{ctx.author.display_name}'s profile", colour=discord.Colour.green())
    #     em.add_field(inline=False, name="Status:", value=ctx.author.status)
    #     em.add_field(inline=False, name="Activity:", value=f"{custom if custom else ''}{activity} " if ctx.author.activities else"No activity")
    #     em.add_field(inline=False, name="Joined:", value=f"{ctx.author.joined_at}")
    #     em.add_field(inline=False, name="Roles:", value="\n".join([i.mention for i in ctx.author.roles]) if len(ctx.author.roles) <= 5 else ", ".join([i.mention for i in ctx.author.roles]))
    #
    #     await ctx.send(embed=em)


def setup(bot):
    bot.add_cog(Other(bot))
