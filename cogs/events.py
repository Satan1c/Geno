# -*- coding: utf-8 -*-

import re

import discord
from discord.ext import commands as cmd


class Events(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.config = bot.servers
        self.DB = bot.DataBase

    @cmd.Cog.listener("on_guild_join")
    async def guild_join(self, guild: discord.Guild):
        await self.DB(self.bot).create_server(guild)
        
    @cmd.Cog.listener("on_member_update")
    async def member_update(self, before: discord.Member, after: discord.Member):
        pass
    
    @cmd.Cog.listener("on_user_update")
    async def user_update(self, before: discord.User, after: discord.User):
        pass
    
    @cmd.Cog.listener("on_member_join")
    async def member_join(self, member: discord.Member):
        pass
    
    @cmd.Cog.listener("on_member_remove")
    async def member_remove(self, member: discord.Member):
        pass

    @cmd.Cog.listener("on_voice_state_update")
    async def voice_update(self, member: discord.Member, before, after):
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

    @cmd.Cog.listener("on_message")
    async def message(self, message: discord.Message):
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

    @cmd.Cog.listener("on_raw_reaction_add")
    async def reaction_add(self, payload: discord.RawReactionActionEvent):
        if not payload.guild_id: return

        cfg = self.bot.servers.find_one({"_id": str(payload.guild_id)}) if payload.guild_id else None

        if payload.member and payload.member.bot or str(payload.message_id) not in cfg['rroles']: return

        message = await payload.member.guild.get_channel(payload.channel_id).fetch_message(payload.message_id)

        if str(payload.message_id) in cfg['rroles']['reaction_remove_msgs']:
            await message.remove_reaction(payload.emoji, payload.member)

        roles = [i.id for i in payload.member.roles]
        for role in cfg['rroles'][str(payload.message_id)][payload.emoji.name]['roles']:
            if role not in roles:
                await payload.member.add_roles(int(role))

    @cmd.Cog.listener("on_raw_reaction_remove")
    async def reaction_remove(self, payload: discord.RawReactionActionEvent):
        cfg = self.bot.servers.find_one({"_id": str(payload.guild_id)})
        if not payload.guild_id or str(payload.message_id) not in cfg['rroles']:
            return

        guild = self.bot.get_guild(payload.guild_id)
        if not payload.member:
            payload.member = guild.get_member(payload.user_id)

        if payload.member and payload.member.bot:
            return

        message = await guild.get_channel(payload.channel_id).fetch_message(payload.message_id)

        if str(payload.message_id) in cfg['rroles']['reaction_remove_msgs']:
            await message.remove_reaction(payload.emoji, payload.member)

        roles = [str(i.id) for i in payload.member.roles]
        for role in cfg['rroles'][str(payload.message_id)][payload.emoji.name]['roles']:
            if str(role) in roles:
                await payload.member.remove_roles(int(role))


def setup(bot):
    bot.add_cog(Events(bot))
