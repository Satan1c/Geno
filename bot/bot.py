# -*- coding: utf-8 -*-
import os
from datetime import datetime

from motor.motor_asyncio import AsyncIOMotorClient

import config
import discord
from discord.ext import commands as cmd
from . import models
from .utils import Utils, Paginator, DataBase, EmbedGenerator, Twitch, Checks

intents = discord.Intents.all()
intents.presences = False


class Geno(cmd.AutoShardedBot):
    prefix = "g-"

    def __init__(self):
        super().__init__(cmd.when_mentioned_or(self.get_prefix),
                         owner_id=348444859360608256,
                         intents=intents,
                         case_insensitive=True,
                         help_command=None,
                         fetch_offline_members=False,
                         max_messages=200)

        # self.prefix = "t-"
        self.prefix = "g-"
        self.version = "(v2.0.0a)"
        self.token = config.TOKEN

        self.models = models
        self.EmbedGenerator = EmbedGenerator
        self.DataBase = DataBase
        self.Paginator = Paginator

        self.client = AsyncIOMotorClient(config.MONGO)
        self.main = self.client.get_database("cfg").get_collection("main")
        self.cmds = self.client.get_database("servers").get_collection("commands")
        self.servers = self.client.get_database("servers").get_collection("configs")
        self.profiles = self.client.get_database("users").get_collection("profiles")
        self.streamers = self.client.get_database("servers").get_collection("streamers")

    async def on_ready(self):
        self.raw_main = await self.main.find_one()

        self.twitch = Twitch(self)
        self.checks = Checks(self)
        self.utils = Utils(self)

        for file in os.listdir('./cogs'):
            if file[-3:] == '.py':
                try:
                    self.load_extension(f'cogs.{file[0:-3]}')
                    print(f'[+] cogs.{file[0:-3]}')
                except BaseException as err:
                    print(f'[!] cogs.{file[0:-3]} error: `{err}`')
                    # raise err

        print('-' * 30)

        await self.change_presence(status=discord.Status.online,
                                   activity=discord.Activity(name=f"{self.prefix}help | {self.version}",
                                                             type=discord.ActivityType.listening))

        print(f"{self.user.name}, is ready")

    async def on_command_error(self, ctx: cmd.Context, err):
        # await ctx.send(err)

        if isinstance(err, cmd.CommandNotFound) or (ctx.command and ctx.command.name == "Help"):
            return

        print("\n", "-" * 30, "\n", err, "\n", "-" * 30, "\n")
        s = str(err).split(": ")
        em = discord.Embed(title=f"{ctx.command.name} ERROR",
                           description=f"{s[0]}: {s[-1]}" if len(s) > 1 else s[0],
                           colour=discord.Colour.red())

        if isinstance(err, cmd.NoPrivateMessage):
            em.description = "Not a guild"

        try:
            print("\n", "-" * 30,
                  f"\n[!] Command error:\n"
                  f"Name: {ctx.command.name}\n"
                  f"Usage: {ctx.message.content}\n"
                  f"User: {str(ctx.author)}\n"
                  f"Server: {ctx.guild.name}  {ctx.guild.id}\n"
                  f"Error: {err}",
                  "-" * 30, "\n")
            await ctx.send(embed=em)
        except BaseException as err:
            print(f"[!] error send error:\n{err}\n", "-" * 30, "\n")

    async def on_command(self, ctx: cmd.Context):
        if not ctx.guild:
            return

        print("\n", "-" * 30,
              f"\n[+] Command usage:\n"
              f"Name: {ctx.command.name}\n"
              f"Usage: {ctx.message.content}\n"
              f"User: {str(ctx.author)}\n"
              f"Server: {ctx.guild.name}  {ctx.guild.id}\n",
              "-" * 30, "\n")
        try:
            await ctx.message.delete()
        except:
            pass

    async def on_connect(self):
        await self.main.update_one({"_id": 0}, {"$set": {"uptime": datetime.utcnow()}})

    async def get_prefix(self, message):
        prefix = self.prefix
        # return prefix

        if message.guild:
            prefix = await self.servers.find_one({"_id": f"{message.guild.id}"})
            prefix = prefix['prefix'] if prefix else self.prefix

        return prefix

    def run(self):
        super().run(self.token)


bot = Geno()
