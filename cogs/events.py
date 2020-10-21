# -*- coding: utf-8 -*-

import re

import discord
from discord.ext import commands as cmd


class Events(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.config = bot.servers
        self.DB = bot.DataBase

    @cmd.Cog.listener()
    async def on_guild_join(self, guild: discord.Guild):
        await self.DB(self.bot).create_server(guild)

    # @cmd.Cog.listener()
    # async def on_member_join(self, member: discord.Member):
    #     await self.DB(self.bot).create_user(member)

    @cmd.Cog.listener()
    async def on_voice_state_update(self, member: discord.Member, before, after):
        cfg = self.config.find_one({"_id": f"{member.guild.id}"})['music']
        if member.id == self.bot.user.id and after.channel and member.voice and not member.voice.deaf:
            try:
                await member.edit(deafen=True)
            except BaseException as err:
                print(err)
                pass

        if member.id == member.guild.me.id and not after.channel and cfg['now_playing']:
            cfg['queue'] = []
            cfg['now_playing'] = ""
            self.config.update_one({"_id": f"{member.guild.id}"}, {"$set": {"music": dict(cfg)}})

        del cfg

    @cmd.Cog.listener()
    async def on_message(self, message: discord.Message):
        if re.sub(r'[^@<>#A-Za-z0-9]', r'', message.content) in [f"{str(self.bot.user)}",
                                                                 f"<@{str(self.bot.user)}>",
                                                                 f"@{str(self.bot.user)}",
                                                                 f"<@{self.bot.user.id}>"]:
            ctx = cmd.Context(bot=self.bot,
                              message=message,
                              guild=message.guild,
                              send=message.channel.send,
                              prefix="-" if not message.guild else self.config.find_one({"_id": f"{message.guild.id}"})[
                                  'prefix'])
            return await self.bot.get_command("Help").callback(ctx=ctx,
                                                               self=self.bot.get_cog("System"), )

        if message.author.id != self.bot.user.id or not message.guild:
            return

        em = discord.Embed()
        if not message.embeds or isinstance(message.embeds[0].image.url, type(em.image.url)):
            await message.delete(delay=120)

    @cmd.Cog.listener()
    async def on_raw_reaction_add(self, pay: discord.RawReactionActionEvent):
        if not pay.guild_id:
            return

        guild = self.bot.get_guild(int(pay.guild_id))
        if not pay.member or pay.member.bot:
            return

        cfg = self.config.find_one({"_id": f"{pay.guild_id}"})
        if not cfg or 'reactions' not in cfg:
            return

        if f"{pay.emoji.id}" in cfg['reactions'] or pay.emoji.name in cfg['reactions']:
            role = guild.get_role(int(
                cfg['reactions'][f"{pay.emoji.id if str(pay.emoji.id) in cfg['reactions'] else pay.emoji.name}"][0]))
            if not role or pay.message_id != int(
                    cfg['reactions'][f"{pay.emoji.id if str(pay.emoji.id) in cfg['reactions'] else pay.emoji.name}"][
                        1]):
                return

            if role not in pay.member.roles:
                return await pay.member.add_roles(role)

        del cfg

    @cmd.Cog.listener()
    async def on_raw_reaction_remove(self, pay: discord.RawReactionActionEvent):
        if not pay.guild_id:
            return

        guild = self.bot.get_guild(int(pay.guild_id))

        if pay.member and pay.member.bot:
            return

        if not pay.member:
            pay.member = guild.get_member(pay.user_id)

        cfg = self.config.find_one({"_id": f"{pay.guild_id}"})
        if not cfg or 'reactions' not in cfg:
            return

        if f"{pay.emoji.id}" in cfg['reactions'] or f"{pay.emoji.name}" in cfg['reactions']:
            role = guild.get_role(int(
                cfg['reactions'][f"{pay.emoji.id if str(pay.emoji.id) in cfg['reactions'] else pay.emoji.name}"][0]))
            if not role or pay.message_id != int(
                    cfg['reactions'][f"{pay.emoji.id if str(pay.emoji.id) in cfg['reactions'] else pay.emoji.name}"][
                        1]):
                return

            if role in pay.member.roles:
                return await pay.member.remove_roles(role)

        del cfg


def setup(bot):
    bot.add_cog(Events(bot))
