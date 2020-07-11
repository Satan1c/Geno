# -*- coding: utf-8 -*-

import platform
from datetime import datetime

import discord
import psutil
from discord.ext import commands as cmd


class System(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.config = bot.servers
        self.profile = bot.profiles
        self.utils = bot.utils
        self.Paginator = bot.Paginator
        self.reactions = ('⬅', '⏹', '➡')
        self.arrowl = "<a:31:637653092749410304>"
        self.arrowr = "<a:30:637653060726030337>"
        self.bot_invite = "https://discord.com/oauth2/authorize?client_id={id}0&permissions={perms}&scope=bot"
        self.supp_link = "https://discord.gg/NSkg6N9"
        self.patreon_link = "https://patreon.com/satan1c"

    @cmd.command(name="Test", aliases=['test'], hidden=True)
    async def _test(self, ctx: cmd.Context):
        bytes = ctx.voice_client.endpoint
        await ctx.send(bytes)

    @cmd.command(name="Help", hidden=True, aliases=['h', 'help', 'commands', 'cmds'])
    async def _help(self, ctx: cmd.Context):
        prefix = "-" if not ctx.guild else self.config.find_one({"_id": f"{ctx.guild.id}"})['prefix']
        em = discord.Embed(colour=discord.Colour.green(),
                           title=f'{self.arrowl} Commands list, prefix: {prefix} {self.arrowr}',
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
            cmds = [f"{(x + 1) - (len(hided))}. `{listt[x].usage}`" for x in range(len(listt)) if not listt[x].hidden]

            if len(cmds) == 0:
                continue

            cmds = "\n".join(cmds)
            embeds.append(discord.Embed(colour=discord.Colour.green(),
                                        title=f'{self.arrowl} Commands list {self.arrowr}',
                                        description=f"prefix: `{prefix}`")
                          .add_field(name=f"{cog}", value=cmds))

        p = self.Paginator(ctx, embeds=embeds, begin=em)
        await p.start()

    @cmd.command(name="Profile", aliases=['profile', 'prf'], usage="profile")
    @cmd.guild_only()
    async def _profile(self, ctx: cmd.Context):
        prf = self.profile.find_one({"sid": f"{ctx.guild.id}", "uid": f"{ctx.author.id}"})
        em = discord.Embed(title=f"{self.arrowl} {ctx.author.display_name} profile {self.arrowr}",
                           colour=discord.Colour.green(),
                           timestamp=datetime.now())

        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))
        em.add_field(name="messages:", value=f"`{prf['messages']}`")

        return await ctx.send(embed=em)

    @cmd.command(name="Server", aliases=['server', 'srv', 'information', 'info'], usage="server")
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

    @cmd.command(name="Bot", aliases=['bot', 'about'], usage="bot")
    @cmd.guild_only()
    async def _bot(self, ctx: cmd.Context):
        system = platform.uname()
        cpu = f"`cores: {psutil.cpu_count(logical=True)}" \
              f"\nfrequency: {round(psutil.cpu_freq().current / 1000, 1)}ghz" \
              f"\nusage: {psutil.cpu_percent()}%`"
        ram = f"`volume: {(psutil.virtual_memory().available // 1024) // 1000}mb /" \
              f" {(psutil.virtual_memory().total // 1024) // 1000}mb\n" \
              f"percentage: {round(psutil.virtual_memory().available * 100 / psutil.virtual_memory().total, 1)}%`"

        em = discord.Embed(title=f"{self.arrowl} {ctx.me.name} info {self.arrowr}",
                           colour=discord.Colour.green(),
                           timestamp=datetime.now())
        em.add_field(name="OS:", value=f"`{system[0]} {system[2]}`")
        em.add_field(name="CPU:", value=f'`{cpu}`')
        em.add_field(name="RAM:", value=ram)
        em.add_field(name="Users:", value=f"`{len(self.bot.users)}`")
        em.add_field(name="Guilds:", value=f"`{len(self.bot.guilds)}`")
        em.add_field(name='\u200b', value="\u200b")
        em.add_field(name="Up-time:", value=f"`in development`")
        em.add_field(name="Ping:", value=f"`{round(self.bot.latency, 1)}s`")
        em.add_field(name='\u200b', value="\u200b")
        em.add_field(name="Python version:", value=f"`{platform.python_version()}`")
        em.add_field(name="Discord.Py version:",
                     value=f"`{discord.version_info[0]}.{discord.version_info[1]}.{discord.version_info[2]}`")
        em.add_field(name='\u200b', value="\u200b")
        em.add_field(name="Bot invite:", value=f"[Click]({self.bot_invite.format(id=self.bot.user.id, perms=536210647)})")
        em.add_field(name="Support server:", value=f"[Click]({self.supp_link})")
        em.add_field(name="Patreon:", value=f"[Click]({self.patreon_link})")

        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

        await ctx.send(embed=em)


def setup(bot):
    bot.add_cog(System(bot))
