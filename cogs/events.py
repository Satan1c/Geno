# -*- coding: utf-8 -*-

from discord.ext import commands as cmd


class Events(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.DB = bot.DataBase

    @cmd.Cog.listener()
    async def on_ready(self):
        await self.DB(self.bot).create()
        print(f"{self.bot.user.name}, is ready")


def setup(bot):
    bot.add_cog(Events(bot))
