# -*- coding: utf-8 -*-

import platform
from datetime import datetime

import discord
import psutil
from discord.ext import commands as cmd
from asyncio import sleep


class System(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.config = bot.servers
        self.profile = bot.profiles
        self.utils = bot.utils
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
    async def _test(self, ctx: cmd.Context):
        em = discord.Embed(title="Test").to_dict()
        await ctx.send(embed=discord.Embed.from_dict(em))
    
    @cmd.command(name="Announcer", aliases=['announce'], hidden=True)
    @cmd.is_owner()
    async def _announce(self, ctx: cmd.Context, *, text: str):
        print(text)
        owners = [j for i in self.bot.guilds for j in i.members if i.owner_id == j.id]
        for member in owners:
            await member.send(embed=discord.Embed.from_dict(dict(text)))
 
    @cmd.command(name="Help", hidden=True, aliases=['h', 'help', 'commands', 'cmds'])
    async def _help(self, ctx: cmd.Context):
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
    
    @cmd.command(name="Prefix", aliases=['prefix'], usage="prefix <prefix>")
    @cmd.guild_only()
    async def _prefix(self, ctx: cmd.Context, *, prefix: str = "-"):
        cfg = self.config.find_one({"_id": f"{ctx.guild.id}"})
        raw = cfg['prefix']
        if raw == prefix:
            raise cmd.BadArgument("New prefix can't be equals old")

        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"prefix": prefix}})
        await ctx.send(embed=discord.Embed(title="Prefix change",
        description=f"From: `{raw}`\nTo: `{prefix}`",
        colour=discord.Colour.green(),
        timestamp=datetime.now())
        .set_footer(text=str(ctx.author), icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256)))


    @cmd.command(name="Bot", aliases=['bot', 'about'], usage="bot")
    async def _bot(self, ctx: cmd.Context):
        system = platform.uname()
        cpu = f"`cores: {psutil.cpu_count(logical=True)}" \
              f"\nfrequency: {round(psutil.cpu_freq().current / 1000, 1)}ghz" \
              f"\nusage: {psutil.cpu_percent()}%`"
        ram = f"`volume: {(psutil.virtual_memory().available // 1024) // 1000}mb /" \
              f" {(psutil.virtual_memory().total // 1024) // 1000}mb\n" \
              f"percentage: {round(psutil.virtual_memory().available * 100 / psutil.virtual_memory().total, 1)}%`"

        em = self.EmbedGenerator(target="bot", ctx=ctx, system=system, cpu=cpu, ram=ram, platform=platform, data=self).get()
        await ctx.send(embed=em)


def setup(bot):
    bot.add_cog(System(bot))
