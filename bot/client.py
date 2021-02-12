# -*- coding: utf-8 -*-
import os

import lavalink
import pymongo
from google_trans_new import google_translator
from motor.motor_asyncio import AsyncIOMotorClient

import discord
from config import TOKEN, MONGO
from discord.ext import commands as cmd
from discord.gateway import IdentifyConfig
from . import models
from .utils import Twitch, Utils, Paginator, Checks, DataBase, EmbedGenerator

translator = google_translator()

intents = discord.Intents.all()
intents.presences = False
IdentifyConfig.browser = 'Discord Android'


class Geno(cmd.Bot):
    version = "(v2.0.0b)"
    prefix = "g-"
    client: pymongo.MongoClient = AsyncIOMotorClient(MONGO)
    main_cfg: pymongo.collection.Collection = client.get_database("cfg").get_collection("main")
    servers: pymongo.collection.Collection = client.get_database("servers").get_collection("configs")
    lavalink = lavalink.Client

    def __init__(self):
        super().__init__(command_prefix=self.get_prefix,
                         owner_ids=(348444859360608256,),
                         case_insensitive=True,
                         intents=intents)

    async def init(self):
        self.remove_command('help')
        self.raw_main = await self.main_cfg.find_one()
        self.twitch = Twitch(self)
        self.Paginator = Paginator
        self.DataBase = DataBase
        self.EmbedGenerator = EmbedGenerator
        self.models = models
        self.cmds = self.client.get_database("servers").get_collection("commands")
        self.profiles = self.client.get_database("users").get_collection("profiles")
        self.streamers = self.client.get_database("servers").get_collection("streamers")
        self.checks = Checks(self)
        self.utils = Utils(self)

        for file in os.listdir('./cogs'):
            if file[-3:] == '.py':
                try:
                    self.load_extension(f'cogs.{file[0:-3]}')
                    print(f'[+] cogs.{file[0:-3]}')
                except BaseException as err:
                    print(f'[!] cogs.{file[0:-3]} error: `{err}`')
        print('-' * 30)

    async def on_ready(self):

        await self.init()
        act = discord.Activity(name=f"{Geno.prefix}help | {Geno.version}",
                               type=discord.ActivityType.listening)
        await self.change_presence(status=discord.Status.online, activity=act)

        print(f"{self.user.name}, is ready")

    async def get_prefix(self, message):
        def when_mentioned_or(*prefixes):
            def inner(bot, msg):
                return [bot.user.mention, f'{bot.user.mention} ', f'<@!{bot.user.id}> ', f'<@!{bot.user.id}>'] + list(
                    prefixes)

            return inner

        extras = [Geno.prefix]
        return when_mentioned_or(*extras)(self, message)

    async def on_command_error(self, ctx: cmd.Context, err):
        # raise err
        if not ctx.guild or ctx.author.bot or not ctx.command or ctx.command.hidden or \
                isinstance(err, cmd.CommandNotFound) or \
                str(err) == "DISABLED" or (ctx.command and ctx.command.name == "Help"):
            return

        em = discord.Embed(title=f"Ошибка команды - {ctx.command.name}",
                           description=translator.translate(str(err).split(": ")[-1], lang_tgt="ru"),
                           colour=discord.Colour.red())
        em.set_footer(text=f"")

        if isinstance(err, cmd.NoPrivateMessage):
            em.description = "Не сервер"

        try:
            print("\n[!] Command error:\n"
                  f"Name: {ctx.command.name}\n"
                  f"Usage: {ctx.message.content}\n"
                  f"User: {str(ctx.author)}\n"
                  f"Server: {ctx.guild.name}  {ctx.guild.id}\n"
                  f"Error: {err}\n")
            await ctx.send(embed=em)
        except BaseException as err:
            print(f"\n[!] error send error:\n{err}\n")

    @staticmethod
    async def on_command(ctx: cmd.Context):
        if not ctx.guild:
            return

        try:
            await ctx.message.delete()
        except:
            pass

    def run(self):
        super().run(TOKEN)


geno = Geno()
