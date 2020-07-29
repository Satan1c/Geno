# -*- coding: utf-8 -*-

import os
from asyncio import sleep
from datetime import datetime

import pymongo
from requests import post

import config
import discord
from config import SDC, Boat
from discord.ext import commands as cmd
from discord.gateway import IdentifyConfig
from . import models
from .utils import Utils, Paginator, DataBase, EmbedGenerator, Twitch

IdentifyConfig.browser = 'Discord Android'

client = pymongo.MongoClient(config.MONGO)


async def req(bot_client):
    urls = [{"url": f"https://api.server-discord.com/v2/bots/{bot_client.user.id}/stats", "token": f"SDC {SDC}",
             "servers": "servers"},
            {"url": f"https://discord.boats/api/bot/{bot_client.user.id}",
             "token": f"{Boat}", "servers": "server_count"}]
    while 1:
        for i in urls:
            headers = {
                "Authorization": i['token']
            }
            data = {
                i['servers']: len(bot_client.guilds)
            }
            if i['token'].startswith("SDC "):
                data['shards'] = 1

            post(url=i['url'], data=data, headers=headers)

        await sleep(901)


class Geno(cmd.Bot):
    def __init__(self):
        super().__init__(command_prefix=self.get_prefix, owner_id=348444859360608256)
        self.token = config.TOKEN
        # self.prefix = "t-"
        self.prefix = "-"
        self.version = "(v0.1.4a)"
        self.main = client.cfg.main

    def init(self):
        self.servers = client.servers.configs
        self.streamers = client.servers.streamers
        self.profiles = client.users.profiles
        self.models = models
        self.twitch = Twitch()
        self.EmbedGenerator = EmbedGenerator
        self.DataBase = DataBase
        self.Paginator = Paginator
        self.utils = Utils(self)

        self.load_extension("jishaku")
        self.remove_command('help')

        for file in os.listdir('./cogs'):
            if file[-3:] == '.py':
                try:
                    self.load_extension(f'cogs.{file[0:-3]}')
                    print(f'[+] cogs.{file[0:-3]}')
                except BaseException as err:
                    print(f'[!] cogs.{file[0:-3]} error: `{err}`')
        print('-' * 30)

    async def on_ready(self):
        self.init()
        act = discord.Activity(name=f"-help | {self.version}",
                               type=discord.ActivityType.listening)
        await self.change_presence(status=discord.Status.online, activity=act)

        # await self.get_guild(648571219674923008).get_channel(648780121419022336).send("Ready")
        await self.DataBase(self).create()
        print(f"{self.user.name}, is ready")

        await self.utils.check_twitch()
        await req(self)

    async def on_command_error(self, ctx: cmd.Context, err):
        # raise err
        if isinstance(err, cmd.CommandNotFound):
            return

        em = discord.Embed(title=f"{ctx.command.name} ERROR",
                           description=str(err),
                           colour=discord.Colour.red())

        if isinstance(err, cmd.NoPrivateMessage):
            em.description = "Not a giuld"

        await ctx.send(embed=em)

    @staticmethod
    async def on_command(ctx: cmd.Context):
        if isinstance(ctx.channel, discord.DMChannel):
            return
        await ctx.message.delete()

    async def on_connect(self):
        # return
        self.main.update_one({"_id": 0}, {"$set": {"uptime": datetime.now()}})

    async def get_prefix(self, message):
        prefix = self.prefix
        # return prefix

        if message.guild:
            prefix = self.servers.find_one({"_id": f"{message.guild.id}"})['prefix']

        return prefix

    def run(self):
        super().run(self.token)


bot = Geno()
