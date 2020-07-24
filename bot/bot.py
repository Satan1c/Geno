# -*- coding: utf-8 -*-

import os
from datetime import datetime

import discord
import pymongo
from discord.ext import commands as cmd

import config
from . import models
from .utils import Utils, Video, Paginator, DataBase, EmbedGenerator

client = pymongo.MongoClient(config.MONGO)


class Geno(cmd.Bot):
    def __init__(self):
        super().__init__(command_prefix=self.get_prefix, owner_id=348444859360608256)
        self.init()

    def init(self):
        self.token = config.TOKEN
        # self.prefix = "t-"
        self.prefix = "-"
        self.version = "(v0.1.3a)"
        self.servers = client.servers.configs
        self.profiles = client.users.profiles
        self.main = client.cfg.main
        self.models = models
        self.utils = Utils(self)
        self.Video = Video
        self.EmbedGenerator = EmbedGenerator
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
        try:
            m = await ctx.send(embed=em)
            await m.delete(delay=120)
        exept:
            pass

    async def on_command(self, ctx: cmd.Context):
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


bot = Geno()
