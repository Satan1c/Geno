# -*- coding: utf-8 -*-

import discord
from discord.ext import commands as cmd


class Events(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.config = bot.servers
        self.DB = bot.DataBase

    @cmd.Cog.listener()
    async def on_ready(self):
        await self.DB(self.bot).create()
        print(f"{self.bot.user.name}, is ready")
        await self.bot.get_guild(648571219674923008).get_channel(648780121419022336).send("Ready")
    
    @cmd.Cog.listener()
    async def on_guild_join(self, guild: discord.Guild):
        await self.DB.create_server(guild)
        if not guild.me.deafen:
            await guild.me.edit(deafen=True)

    @cmd.Cog.listener()
    async def on_member_join(self, member: discord.Member):
        await self.DB.create_user(member)

    @cmd.Cog.listener()
    async def on_voice_state_update(self, member: discord.Member, before, after):
        cfg = self.config.find_one({"_id": f"{member.guild.id}"})['music']
        if member.id == self.bot.user.id and after.channel and member.voice and not member.voice.deaf:
            await member.edit(deafen=True)
        
        if member.id == member.guild.me.id and not after.channel and cfg['now_playing']:
            cfg['queue'] = []
            cfg['now_playing'] = ""
            self.config.update_one({"_id": f"{member.guild.id}"}, {"$set": {"music": dict(cfg)}})


def setup(bot):
    bot.add_cog(Events(bot))
