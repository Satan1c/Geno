# -*- coding: utf-8 -*-

import discord
from discord.ext import commands as cmd
from asyncio import sleep
from requests import post
from config import SDC, Boat

async def req(bot):
    urls = [{"url": f"https://api.server-discord.com/v2/bots/{bot.user.id}/stats", "token": f"SDC {SDC}", "servers": "servers"},
            {"url": f"https://discord.boats/api/bot/{bot.user.id}", "token": f"{Boat}", "servers": "server_count"}]
    while 1:
        for i in urls:
            headers = {
                "Authorization": i['token']
            }
            data = {
                i['servers']: len(bot.guilds)
            }
            if i['token'].startswith("SDC "):
                data['shards'] = 1
            
            post(url=i['url'], data=data, headers=headers)
            
        await sleep(901)


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

        await req(self.bot)

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


def setup(bot):
    bot.add_cog(Events(bot))
