# -*- coding: utf-8 -*-
import platform
import re

import psutil

import discord
from bot.client import geno
from discord.ext import commands as cmd

url_rx = re.compile(r'https?://(?:www\.)?.+')
checks = geno.checks


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
    @cmd.cooldown(1, 5, cmd.BucketType.guild)
    async def help(self, ctx: cmd.Context, *, command: str = None):
        if command:
            cmds = [i for j in self.bot.cogs for i in self.bot.cogs[j].walk_commands() if not i.hidden and command.lower() in i.aliases]
            if len(cmds) == 0:
                raise cmd.BadArgument('Ничего не найдено')

            command = cmds[0]
            desc = command.description
            await ctx.send(embed=discord.Embed(title=f"{command.name} help:",
                                               description=f"""
                                                    <> - обязательные параметры, [] -  другие параметры

                                                    Использование команды: {command.usage}
                                                    Иные виды использования: `{", ".join(command.aliases)}`

                                                    {desc}""",

                                               colour=discord.Colour.green()))
            return

        prefix = self.bot.prefix if not ctx.guild else await self.config.find_one({"_id": f"{ctx.guild.id}"})
        if not isinstance(prefix, str):
            prefix = prefix['prefix']

        em = discord.Embed(colour=discord.Colour.green(),
                           title=f'Commands list',
                           description=f"""prefix: `{prefix}`

                               поставьте {self.reactions[0]} для шага нгазад
                               поставьте {self.reactions[1]} для закрытия \"help\"
                               поставьте {self.reactions[2]} для шага вперед""")
        embeds = []

        for cog in self.bot.cogs:
            listt = [i for i in self.bot.cogs[cog].walk_commands() if not i.hidden]
            cmds = [f"`{x + 1}`. {listt[x].usage}" for x in range(len(listt))]

            if len(cmds) == 0:
                continue

            cmds = "\n".join(cmds)
            embeds.append(discord.Embed(colour=discord.Colour.green(),
                                        title="Список команд",
                                        description=f"Префикс: `{prefix}`\nhelp `['использование команды']`"
                                                    f" - для помощи по одной команде")
                          .add_field(name=f"{cog}", value=cmds))
        p = self.Paginator(ctx, embeds=embeds, begin=em)
        await p.start()

    @cmd.command(name="Bot info command", aliases=['about', 'бот', 'bot'], usage="bot", description="""
        Показывает некотороую информацию про меня
        """)
    @cmd.check(checks.is_off)
    @cmd.cooldown(1, 2, cmd.BucketType.user)
    async def info_bot(self, ctx: cmd.Context):
        system = platform.uname()
        proc = psutil.Process()

        with proc.oneshot():
            cpu = f"`ядра: {psutil.cpu_count()}" \
                  f"\nчастота: {round(psutil.cpu_freq().current / 1000, 1)}ghz" \
                  f"\nзагрузка: {psutil.cpu_percent()}%`"

            mem = proc.memory_full_info()
            ram = f"`используемый объем: {round((mem.vms // 1024) / 1024, 1)}mb`"

            em = await self.EmbedGenerator.init(target="bot", ctx=ctx, system=system, cpu=cpu, ram=ram,
                                                platform=platform,
                                                data=self)
            await ctx.send(embed=em)


def setup(bot):
    bot.add_cog(Other(bot))
