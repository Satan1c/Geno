# -*- coding: utf-8 -*-

import re
from datetime import datetime

import discord
from discord.ext import commands as cmd


class Moderation(cmd.Cog):
    def __int__(self, bot):
        self.bot = bot
        self.config = bot.servers

    @cmd.command(name="Ban", aliases=['ban', 'b'], usage="ban <user> [reason]")
    @cmd.guild_only()
    async def _ban(self, ctx: cmd.Context, user, *, reason: str = "no reason"):
        embed = discord.Embed(title="Ban",
                                   description="User: {user}\nReason: {reason}",
                                   timestamp=datetime.now(),
                                   colour=discord.Colour.green())
        embed.set_author(name=str(ctx.author),
                              icon_url=ctx.author.avatar_url_as(
                                  format="png",
                                  static_format='png',
                                  size=256))

        if (ctx.message.mentions and ctx.message.mentions[0].mention != re.sub('!', '', user)) \
                and len(re.sub(r"[^0-9]", r"", user)) == 18:
            user = discord.Object(id=int(user))
            await ctx.guild.ban(user, reason=reason)

            bans = await ctx.guild.bans()
            bans = [f"{i.user.name}#{i.user.discriminator}" for i in bans if i.user.id == user.id][0]

            embed.description = embed.description.format(user=bans, reason=reason)
            embed.title = "Soft Ban"

            m = await ctx.send(embed=embed)
            await m.delete(delay=120)
            return

        user = ctx.message.mentions[0]

        await ctx.guild.ban(user, reason=reason)
        embed.description = embed.description.format(user=str(user), reason=reason)

        m = await ctx.send(embed=embed)
        await m.delete(delay=120)

    @cmd.command(name="Kick", aliases=['kick', 'k'], usage="kick <user> [reason]")
    @cmd.guild_only()
    async def _kick(self, ctx: cmd.Context, user: discord.Member, *, reason: str = "no reason"):
        discord.Embed(title="Kick",
                                   description="User: {user}\nReason: {reason}",
                                   timestamp=datetime.now(),
                                   colour=discord.Colour.green())
        embed.set_author(name=str(ctx.author),
                              icon_url=ctx.author.avatar_url_as(
                                  format="png",
                                  static_format='png',
                                  size=256))

        await ctx.guild.kick(user, reason=reason)
        m = await ctx.send(embed=embed)
        await m.delete(delay=120)


def setup(bot):
    bot.add_cog(Moderation(bot))
