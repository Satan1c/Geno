# -*- coding: utf-8 -*-
import os
from datetime import datetime

import lavalink
from motor.motor_asyncio import AsyncIOMotorClient

import config
import discord
from discord.ext import commands as cmd
from discord.gateway import IdentifyConfig
from . import models
from .utils import Utils, Paginator, DataBase, EmbedGenerator, Twitch, Checks

IdentifyConfig.browser = 'Discord Android'

intents = discord.Intents.all()
intents.presences = False


class Geno(cmd.Bot):
    prefix = "g-"

    def __init__(self):
        super().__init__(cmd.when_mentioned_or(self.get_prefix),
                         owner_id=348444859360608256,
                         intents=intents,
                         case_insensitive=True)
        self.client = AsyncIOMotorClient(config.MONGO)
        self.token = config.TOKEN
        self.prefix = "t-"
        # self.prefix = "g-"
        self.version = "(v1.0.1)"
        self.main = self.client.get_database("cfg").get_collection("main")
        self.servers = self.client.get_database("servers").get_collection("configs")

    async def init(self):
        self.raw_main = await self.main.find_one()

        self.twitch = Twitch(self)
        self.Paginator = Paginator
        self.DataBase = DataBase
        self.EmbedGenerator = EmbedGenerator
        self.models = models
        self.cmds = self.client.get_database("servers").get_collection("commands")
        # self.webhooks = self.client.get_database("servers").get_collection("webhooks")
        self.profiles = self.client.get_database("users").get_collection("profiles")
        self.streamers = self.client.get_database("servers").get_collection("streamers")
        self.checks = Checks(self)
        self.utils = Utils(self)

        # self.lavalink = lavalink.Client(bot.user.id)
        # self.lavalink.add_node('localhost', 8080, 'lavalavago', 'eu', 'music-node')
        # self.add_listener(bot.lavalink.voice_update_handler, 'on_socket_response')

        self.remove_command('help')

        for file in os.listdir('./cogs'):
            if file[-3:] == '.py':
                try:
                    self.load_extension(f'cogs.{file[0:-3]}')
                    print(f'[+] cogs.{file[0:-3]}')
                except BaseException as err:
                    print(f'[!] cogs.{file[0:-3]} error: `{err}`')
                    raise err
        print('-' * 30)

    async def on_ready(self):
        await self.init()
        act = discord.Activity(name=f"{self.prefix}help | {self.version}",
                               type=discord.ActivityType.listening)
        await self.change_presence(status=discord.Status.online, activity=act)

        print(f"{self.user.name}, is ready")

    async def on_command_error(self, ctx: cmd.Context, err):
        if isinstance(err, cmd.CommandNotFound) or (ctx.command and ctx.command.name == "Help"):
            return
        s = str(err).split(": ")
        em = discord.Embed(title=f"{ctx.command.name} ERROR",
                           description=f"{s[0]}: {s[-1]}" if len(s) > 1 else s[0],
                           colour=discord.Colour.red())

        if isinstance(err, cmd.NoPrivateMessage):
            em.description = "Not a giuld"

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
        except BaseException as err:
            print("\n", "-" * 30, f"[!] command call delete error:\n{err}\n", "-" * 30, "\n")

    # @staticmethod
    # async def on_error(event_method, *args, **kwargs):
    #     print("\n", "-" * 30, f"\n[!] unknown error:\n{event_method}\n{args}\n{kwargs}\n", "-" * 30, "\n")

    async def on_connect(self):
        return
        await self.main.update_one({"_id": 0}, {"$set": {"uptime": datetime.now()}})

    async def get_prefix(self, message):
        prefix = self.prefix
        return prefix

        if message.guild:
            prefix = await self.servers.find_one({"_id": f"{message.guild.id}"})
            prefix = prefix['prefix'] if prefix else self.prefix

        return prefix

    def run(self):
        super().run(self.token)


bot = Geno()
