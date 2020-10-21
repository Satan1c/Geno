# -*- coding: utf-8 -*-
import os
from datetime import datetime

import pymongo

import config
import discord
from discord.ext import commands as cmd
from discord.gateway import IdentifyConfig
from . import models
from .utils import Utils, Paginator, DataBase, EmbedGenerator, Twitch, Checks

IdentifyConfig.browser = 'Discord Android'

client = pymongo.MongoClient(config.MONGO)


class Geno(cmd.Bot):
    def __init__(self):
        super().__init__(command_prefix=self.get_prefix, owner_id=348444859360608256)
        self.token = config.TOKEN
        self.prefix = "t-"
        #self.prefix = "g-"
        self.version = "(v0.2.1a)"
        self.main = client.cfg.main
        self.servers = client.servers.configs

    async def on_disconnect(self):
        print(f"{self.user.name} disconnect")

    def init(self):
        self.twitch = Twitch()
        self.Paginator = Paginator
        self.DataBase = DataBase
        self.EmbedGenerator = EmbedGenerator
        self.models = models
        self.cmds = client.servers.commands
        self.webhooks = client.servers.webhooks
        self.profiles = client.users.profiles
        self.streamers = client.servers.streamers
        self.checks = Checks(self)
        self.utils = Utils(self)

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
        act = discord.Activity(name=f"{self.prefix}help | {self.version}",
                               type=discord.ActivityType.listening)
        await self.change_presence(status=discord.Status.online, activity=act)

        print(f"{self.user.name}, is ready")

    async def on_command_error(self, ctx: cmd.Context, err):
        raise err
        if isinstance(err, cmd.CommandNotFound) or (not ctx.guild and ctx.command.name == "Help"):
            return

        em = discord.Embed(title=f"{ctx.command.name} ERROR",
                           description=str(err),
                           colour=discord.Colour.red())

        if isinstance(err, cmd.NoPrivateMessage):
            em.description = "Not a giuld"

        try:
            await ctx.send(embed=em)
        except BaseException as err:
            print(err)
            pass

    @staticmethod
    async def on_command(ctx: cmd.Context):
        if not ctx.guild:
            return
        try:
            await ctx.message.delete()
        except BaseException as err:
            print(err)
            pass

    async def on_connect(self):
        # return
        self.main.update_one({"_id": 0}, {"$set": {"uptime": datetime.now()}})

    async def get_prefix(self, message):
        prefix = self.prefix
        return prefix

        if message.guild:
            prefix = self.servers.find_one({"_id": f"{message.guild.id}"})
            prefix = prefix['prefix'] if prefix else self.prefix

        return prefix

    def run(self):
        super().run(self.token)


bot = Geno()
