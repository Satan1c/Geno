import asyncio
import aiohttp

from bot.bot import Geno
from config import SDC, Boat
from discord.ext import commands as cmd
from discord.ext.tasks import loop


class Tasks(cmd.Cog):
    def __init__(self, bot: Geno):
        self.bot = bot
        self.streamers = bot.streamers
        self.twitch = bot.twitch
        self.SDC = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjY0ODU3MDM0MTk3NDczNjkyNiIsImlhdCI6MTU5NTYxMzU0NX0.8hV9VHaZ07W1GPyRo4JX9ujyWSzHvmxgc_yTIIzEMUI"
        self.Boat = "EJ665CuJlCMEBImkQ3sinuJAfO06Z3zXLagNj1uzvyxEWkZkl8umxV3Oo9BGl3I7EArTTsnbAhGz1x75xU23TEbaeIsRcuP2atjOA0Ov6HTULjktaUKmiJHyW9twVFdj79EzQikTN6iAz8HN4Eoc7p4YVD0"
        self.Boticord_t = "74992d73-5f95-441a-abb9-67bd4b27b883"
        self.Boticord_u = "https://boticord.top/api/stats?servers={servers}&shards={shards}&users={users}"

        ls = [self.check_twitch, self.db_update, self.monitors_update]
        for i in ls:
            i.start()

    @loop(hours=1)
    async def monitors_update(self):
        tasks = []
        async with aiohttp.ClientSession() as session:
            tasks.append(self.sdc(session))
            tasks.append(self.boats(session))
            tasks.append(self.boticord(session))

            await asyncio.gather(*tasks)

        print("monitorings")

    @loop(minutes=10)
    async def check_twitch(self):
        try:
            tasks = []
            async with aiohttp.ClientSession() as session:
                async for streamer in self.streamers.find():
                    tasks.append(self.twitchc(streamer, session))

                await asyncio.gather(*tasks)

        except BaseException as err:
            print("\n", "-" * 30, f"\n[!]Tasks check_twitch error:\n{err}\n", "-" * 30, "\n")

        print("twitch")

    @loop(hours=6)
    async def db_update(self):
        await self.bot.DataBase(self.bot).create()
        print("db")

    async def sdc(self, session):
        res = await session.post(f"https://api.server-discord.com/v2/bots/{self.bot.user.id}/stats",
                                 headers={"Authorization": f"SDC {SDC}"},
                                 data={"shards": self.bot.shard_count or 1, "servers": len(self.bot.guilds)})
        print("SDC:", await res.json())

    async def boats(self, session):
        res = await session.post(f"https://discord.boats/api/bot/{self.bot.user.id}",
                                headers={"Authorization": Boat},
                                data={"server_count": len(self.bot.guilds)})
        print("Boats:", await res.json())

    async def boticord(self, session):
        res = await session.get(self.Boticord_u.format(servers=len(self.bot.guilds),
                                                      users=len(self.bot.users),
                                                      shards=self.bot.shard_count or 1),
                               headers={"Authorization": self.Boticord_t})
        print("Boticord:", await res.json())

    async def twitchc(self, streamer: dict, session):
        if len(streamer['servers']) <= 0:
            await self.streamers.delete_one({"_id": streamer['_id']})
            return

        query = self.twitch.get_stream_query(streamer['_id'])
        res1 = await self.twitch.get_response(query, session)
        res1 = res1['data']

        if len(res1) < 1 or int(res1[0]['id']) == int(streamer['stream_id']):
            return
        res1 = res1[0]

        query = self.twitch.get_user_query(streamer['_id'])
        res2 = await self.twitch.get_response(query, session)
        res2 = res2['data'][0]

        for server in streamer['servers']:
            channel = self.bot.get_guild(int(server['id'])).get_channel(int(server['channel']))
            await self.twitch.stream_embed(streamer['_id'], res1, res2, channel)

        await self.streamers.update_one({"_id": streamer['_id']}, {"$set": {"stream_id": res1['id']}})


def setup(bot):
    bot.add_cog(Tasks(bot))
