
# -*- coding: utf-8 -*-

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

    @cmd.Cog.listener()
    async def on_member_join(self, member: discord.Member):
        await self.DB(self.bot).create_user(member)

    @cmd.Cog.listener()
    async def on_voice_state_update(self, member: discord.Member, before, after):
        cfg = self.config.find_one({"_id": f"{member.guild.id}"})['music']
        if member.id == self.bot.user.id and after.channel and member.voice and not member.voice.deaf:
            try:
                await member.edit(deafen=True)
            except:
                pass

        if member.id == member.guild.me.id and not after.channel and cfg['now_playing']:
            cfg['queue'] = []
            cfg['now_playing'] = ""
            self.config.update_one({"_id": f"{member.guild.id}"}, {"$set": {"music": dict(cfg)}})

    @cmd.Cog.listener()
    async def on_message(self, message: discord.Message):
        if isinstance(message.channel, discord.DMChannel) or len(message.embeds) < 1:
            return

        em = discord.Embed()
        if message.author.id == self.bot.user.id and isinstance(message.embeds[0].image.url, type(em.image.url)):
            await message.delete(delay=120)

    @cmd.Cog.listener()
    async def on_raw_reaction_add(self, pay: discord.RawReactionActionEvent):
        if pay.member.bot:
            return
        cfg = self.config.find_one({"_id": f"{pay.guild_id}"})['reactions']
        if f"{pay.emoji.id}" in cfg:
            guild = self.bot.get_guild(int(pay.guild_id))
            msg = await guild.get_channel(pay.channel_id).fetch_message(pay.message_id)
            role = guild.get_role(int(cfg[f"{pay.emoji.id}"]))
            await msg.remove_reaction(pay.emoji, pay.member)
            if role not in pay.member.roles:
                return await pay.member.add_roles(role)
            else:
                return await pay.member.remove_roles(role)


def setup(bot):
    bot.add_cog(Events(bot))
