# -*- coding: utf-8 -*-

import os
from datetime import datetime

import config
import discord
import pymongo
from discord.ext import commands as cmd

from . import models
from .utils import Utils, Video, Paginator, DataBase

client = pymongo.MongoClient(config.MONGO)


class Geno(cmd.Bot):
    def __init__(self):
        super().__init__(command_prefix=self.get_prefix)
        self.init()

    def init(self):
        self.token = config.TOKEN
        #self.prefix = "t-"
        self.prefix = "-"
        self.version = "(v0.1.2a)"
        self.servers = client.servers.configs
        self.profiles = client.users.profiles
        self.main = client
        self.models = models
        self.utils = Utils(self)
        self.Video = Video
        self.DataBase = DataBase
        self.Paginator = Paginator

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

    async def on_command_error(self, ctx: cmd.Context, err):
        if isinstance(err, cmd.CommandNotFound):
            return

        em = discord.Embed(title=f"{ctx.command.name} ERROR",
                           description=str(err),
                           colour=discord.Colour.red())

        if isinstance(err, cmd.NoPrivateMessage):
            em.description = "Not a giuld"

        return await ctx.send(embed=em)

    async def get_prefix(self, message):
        prefix = self.prefix
        #return prefix

        if message.guild:
            prefix = self.servers.find_one({"_id": f"{message.guild.id}"})['prefix']

        return prefix

    async def on_connect(self):
        self.start = datetime.now()

    def run(self):
        super().run(self.token)


bot = Geno()
