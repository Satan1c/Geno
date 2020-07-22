# -*- coding: utf-8 -*-

import re
from datetime import datetime

import discord
from discord.ext import commands as cmd


class Moderation(cmd.Cog):
    def __int__(self, bot):
        self.bot = bot

    @cmd.command(name="Ban", aliases=['ban', 'b'], usage="ban <user> [reason]")
    @cmd.bot_has_guild_permissions(ban_members=True)
    @cmd.has_guild_permissions(ban_members=True)
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

        user = re.sub(r"[^0-9]", r"", user)

        if len(ctx.message.mentions) > 0:
            user = ctx.guild.get_member(int(ctx.message.mentions[0].id))

        elif len(user) == 18:
            mem = ctx.guild.get_member(int(user))
            if not mem:
                user = discord.Object(id=int(user))
                await ctx.guild.ban(user=user, reason=reason, delete_message_days=0)

                bans = await ctx.guild.bans()
                bans = [f"{i.user.name}#{i.user.discriminator}" for i in bans if i.user.id == user.id][0]

                embed.description = embed.description.format(user=bans, reason=reason)
                embed.title = "ID Ban"

                m = await ctx.send(embed=embed)
                await m.delete(delay=120)
                return

            elif mem:
                user = mem

        if user.id == ctx.bot.owner_id:
            raise cmd.BadArgument("Can't ban my owner")

        await ctx.guild.ban(user, reason=reason, delete_message_days=0)
        embed.description = embed.description.format(user=str(user), reason=reason)

        m = await ctx.send(embed=embed)
        await m.delete(delay=120)

    @cmd.command(name="Kick", aliases=['kick', 'k'], usage="kick <user> [reason]")
    @cmd.bot_has_guild_permissions(kick_members=True)
    @cmd.has_guild_permissions(kick_members=True)
    async def _kick(self, ctx: cmd.Context, user: discord.Member, *, reason: str = "no reason"):
        embed = discord.Embed(title="Kick",
                              description=f"User: {user}\nReason: {reason}",
                              timestamp=datetime.now(),
                              colour=discord.Colour.green())
        embed.set_author(name=str(ctx.author),
                         icon_url=ctx.author.avatar_url_as(
                             format="png",
                             static_format='png',
                             size=256))

        if user.id == ctx.bot.owner_id:
            raise cmd.BadArgument("Can't kick my owner")

        await ctx.guild.kick(user, reason=reason)
        m = await ctx.send(embed=embed)
        await m.delete(delay=120)


def setup(bot):
    bot.add_cog(Moderation(bot))
