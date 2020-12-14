# -*- coding: utf-8 -*-

import re
from datetime import datetime

import discord
from bot.bot import bot as b
from discord.ext import commands as cmd

checks = b.checks


class Moderation(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot

    @cmd.command(name="Ban", aliases=['ban', 'b', 'бан', 'б'], usage="ban `<user>` `[reason]`", description="""
    user - must be user **mention** or **user id**,
     example: <@!348444859360608256>, `348444859360608256`
     
    reason - any text of "reason" that you wanted,
     example: `eat cookies in voice chat`
     default: `no reason`
     
    Can ban user in guild, and can also ban anyone who not in guild
    :-:
    user - должен быть **упоминанием** или **id пользователя**,
     example: <@!348444859360608256>, `348444859360608256`
     
    reason - любой текст "причины",
     пример: `ест печеньки в голосовом канале`
     по умолчанию: `no reason`
    
    Может забанить user на сервере, даже если его нет на нем
    """)
    @cmd.check(checks.is_off)
    @cmd.bot_has_guild_permissions(ban_members=True)
    @cmd.has_guild_permissions(ban_members=True)
    async def ban_command(self, ctx: cmd.Context, user, *, reason: str = "no reason"):
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
        us = await self.bot.fetch_user(int(user)) if user and len(user) >= 17 else None

        if len(ctx.message.mentions) > 0:
            user = ctx.guild.get_member(int(ctx.message.mentions[0].id))

        elif us:
            mem = ctx.guild.get_member(int(user))

            if mem:
                user = mem

            else:
                user = discord.Object(id=user if isinstance(user, str) else user.id)
                if not user:
                    raise cmd.BadArgument("User not found")

                await ctx.guild.ban(user=user, reason=reason, delete_message_days=0)

                bans = await ctx.guild.bans()
                bans = [f"{i.user.name}#{i.user.discriminator}" for i in bans if i.user.id == user.id][0]

                embed.description = embed.description.format(user=bans, reason=reason)
                embed.title = "ID Ban"
        else:
            raise cmd.BadArgument("User not found")

        if user.id == self.bot.owner_id:
            raise cmd.BadArgument("Can't ban my owner")

        await ctx.guild.ban(user, reason=reason, delete_message_days=0)

        if embed.title == "Ban":
            embed.description = embed.description.format(user=str(user), reason=reason)

        await ctx.send(embed=embed)

    @cmd.command(name="Kick", aliases=['kick', 'k', 'кик', 'к'], usage="kick `<user>` `[reason]`", description="""
    user - must be user **mention** or **user id**,
     example: <@!348444859360608256>, `348444859360608256`
     
    reason - any text of "reason" that you wanted,
     example: `eat cookies in voice chat`
     default: `no reason`
     
    Can kick user from guild
    :-:
    user - должен быть **упоминанием** или **id пользователя**,
     example: <@!348444859360608256>, `348444859360608256`
     
    reason - любой текст "причины",
     пример: `ест печеньки в голосовом канале`
     по умолчанию: `no reason`
    
    Может выгнать user с сервера
    """)
    @cmd.check(checks.is_off)
    @cmd.bot_has_guild_permissions(kick_members=True)
    @cmd.has_guild_permissions(kick_members=True)
    async def kick_command(self, ctx: cmd.Context, user: discord.Member, *, reason: str = "no reason"):
        embed = discord.Embed(title="Kick",
                              description=f"User: {user}\nReason: {reason}",
                              timestamp=datetime.now(),
                              colour=discord.Colour.green())
        embed.set_author(name=str(ctx.author),
                         icon_url=ctx.author.avatar_url_as(
                             format="png",
                             static_format='png',
                             size=256))

        if user.id == self.bot.owner_id:
            raise cmd.BadArgument("Can't kick my owner")

        await ctx.guild.kick(user, reason=reason)
        await ctx.send(embed=embed)


def setup(bot):
    bot.add_cog(Moderation(bot))
