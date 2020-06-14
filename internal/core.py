import os

import pymongo
from discord.ext import commands
from discord.ext.commands import CommandNotFound

from .utils import Utils, Video

db = pymongo.MongoClient("mongodb+srv://Geno:Atlas23Game@genodb-wrqdw.mongodb.net/test?retryWrites=true&w=majority")


class Geno(commands.Bot):
    def __init__(self):
        super().__init__(command_prefix="-", case_insensitive=True)
        self.init()

    def init(self):
        self.db = db
        self.token = "NjQ4NTcwMzQxOTc0NzM2OTI2.XrMBwQ.xABpAer6ywRcWSm6xdlivBKa0Q8"
        self.invite_url = "https://discordapp.com/oauth2/authorize?client_id=648570341974736926&permissions=2147483647&scope=bot"
        self.utils = Utils(bot=self)
        self.video = Video

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
        config = db.servers.music
        for i in self.guilds:
            if not config.find_one({"_id": f"{i.id}"}):
                config.insert_one(
                    {"_id": f"{i.id}", "vote_skip": False, "volume": 1.0, "max_volume": 250, "vote_skip_ratio": 0.5,
                     "skip_votes": 0, "now_playing": "",
                     "queue": [], "playlist": {}, "last": {"message": "", "channel": "", "author": ""}})

        else:
            print("ready")

    async def on_command_error(self, ctx, error):
        if isinstance(error, CommandNotFound):
            return
        raise error

    def startup(self):
        super().run(self.token)


core = Geno()
