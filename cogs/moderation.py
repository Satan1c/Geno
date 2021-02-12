# -*- coding: utf-8 -*-

import re
from datetime import datetime

import discord
from bot.client import geno, Geno
from discord.ext import commands as cmd

checks = geno.checks


class Moderation(cmd.Cog):
    def __init__(self, bot: Geno):
        self.bot = bot

    @cmd.command(name="Ban", aliases=['b', 'бан', 'б'], usage="ban `<user>` `[reason]`", description="""
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
    @cmd.cooldown(1, 2, cmd.BucketType.user)
    async def ban(self, ctx: cmd.Context, user, *, reason: str = "no reason"):
        embed = discord.Embed(title="Бан",
                              description="Пользователь: {user}\nПричина: {reason}",
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
                    raise cmd.BadArgument("пользователь не найден")
                if user.id in self.bot.owner_ids:
                    raise cmd.BadArgument("Не могу забань владельца")

                await ctx.guild.ban(user=user, reason=reason, delete_message_days=0)

                bans = await ctx.guild.bans()
                bans = [f"{i.user.name}#{i.user.discriminator}" for i in bans if i.user.id == user.id][0]

                embed.description = embed.description.format(user=bans, reason=reason)
                embed.title = "Бан по id"
                return await ctx.send(embed=embed)
        else:
            raise cmd.BadArgument("Пользователь не найден")

        if user.id in self.bot.owner_ids:
            raise cmd.BadArgument("Не могу забань владельца")

        try:
            await ctx.guild.ban(user, reason=reason, delete_message_days=0)
        except discord.Forbidden:
            raise cmd.BadArgument("Не могу забанить владельца или человека который веше меня по роли")

        if embed.title == "Бан":
            embed.description = embed.description.format(user=str(user), reason=reason)

        await ctx.send(embed=embed)

    @cmd.command(name="Kick", aliases=['k', 'кик', 'к'], usage="kick `<user>` `[reason]`", description="""
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
    @cmd.cooldown(1, 2, cmd.BucketType.user)
    async def kick(self, ctx: cmd.Context, user: discord.Member, *, reason: str = "no reason"):
        embed = discord.Embed(title="Кик",
                              description=f"Пользователь: {user}\nПричина: {reason}",
                              timestamp=datetime.now(),
                              colour=discord.Colour.green())
        embed.set_author(name=str(ctx.author),
                         icon_url=ctx.author.avatar_url_as(
                             format="png",
                             static_format='png',
                             size=256))

        if user.id in self.bot.owner_ids:
            raise cmd.BadArgument("Не могу кикнуть владельца")

        try:
            await ctx.guild.kick(user, reason=reason)
        except discord.Forbidden:
            raise cmd.BadArgument("Не могу икнуть владельца или человека который веше меня по роли")

        await ctx.send(embed=embed)


def setup(bot):
    bot.add_cog(Moderation(bot))
