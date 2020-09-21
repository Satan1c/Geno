import requests

from bot.bot import Geno
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

        self.monitors_update.start()
        self.check_twitch.start()

    @loop(seconds=3601)
    async def monitors_update(self):
        requests.post(url=f"https://api.server-discord.com/v2/bots/{self.bot.user.id}/stats",
                      data={
                          "shards": 1,
                          "servers": len(self.bot.guilds)
                          },
                      headers={"Authorization": f"SDC {self.SDC}"})

        print("SDC")

        requests.post(url=f"https://discord.boats/api/bot/{self.bot.user.id}",
                      data={"server_count": len(self.bot.guilds)},
                      headers={"Authorization": self.Boat})

        print("Boats")

        requests.get(url=self.Boticord_u.format(servers=len(self.bot.guilds),
                                                users=len(self.bot.users),
                                                shards=self.bot.shard_count),
                     headers={"Authorization": self.Boticord_t})

        print("Boticord")

    @loop(seconds=301)
    async def check_twitch(self):
        print("twitch")
        try:
            streamers = [i for i in self.streamers.find()]

            for streamer in streamers:
                query = self.twitch.get_stream_query(streamer['_id'])
                res1 = self.twitch.get_response(query).json()['data']

                if len(res1) < 1 or int(res1[0]['id']) == int(streamer['stream_id']):
                    continue
                res1 = res1[0]
                self.streamers.update_one({"_id": streamer['_id']}, {"$set": {"stream_id": res1['id']}})

                query = self.twitch.get_user_query(streamer['_id'])
                res2 = self.twitch.get_response(query).json()['data'][0]

                for server in streamer['servers']:
                    channel = self.bot.get_guild(int(server['id'])).get_channel(int(server['channel']))
                    await self.twitch.stream_embed(streamer['_id'], res1, res2, channel)

        except BaseException as err:
            print(err)


def setup(bot):
    bot.add_cog(Tasks(bot))