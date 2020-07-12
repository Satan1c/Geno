# -*- coding: utf-8 -*-

import discord
from discord.ext import commands as cmd


class Events(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.DB = bot.DataBase

    @cmd.Cog.listener()
    async def on_ready(self):
        await self.DB(self.bot).create()
        print(f"{self.bot.user.name}, is ready")
    
    @cmd.Cog.listener()
    async def on_guild_join(self, guild: discord.Guild):
        await self.DB.create_server(guild)

    @cmd.Cog.listener()
    async def on_member_join(self, member: discord.Member):
        await self.DB.create_user(member)


def setup(bot):
    bot.add_cog(Events(bot))
