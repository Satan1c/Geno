# -*- coding: utf-8 -*-
import json
import os
from asyncio import TimeoutError
from asyncio import sleep
from datetime import datetime
from math import floor
from typing import Union

import lavalink
import requests
import youtube_dl
from dateutil.relativedelta import relativedelta
from googleapiclient import discovery

import discord
from config import SDC, Boat
from discord import Embed, Colour
from discord.ext import commands as cmd
from discord.ext.commands import Context

YTDL_OPTS = {
    "default_search": "ytsearch",
    "format": "bestaudio/best",
    "quiet": True,
    "no_warnings": True,
    "extract_flat": "in_queue",
    "ignoreerrors": True,
}


class Twitch:
    def __init__(self):
        self.client_id = "zoucwv8yjggvgydciwu6vluq20c533"  # https://dev.twitch.tv/console/apps
        self.token = "zrs93vtu8zxt532qwnnux1cs0jwjhk"  # https://twitchapps.com/tokengen/
        self.url = "https://api.twitch.tv/helix/"

    def get_response(self, query):
        res = requests.get(f"{self.url}{query}",
                           headers={"Client-ID": self.client_id, 'Authorization': f'Bearer {self.token}',
                                    "Accept": "application/vnd.v5+json"})
        return res

    @staticmethod
    async def stream_embed(login, res1, res2, channel):
        em = discord.Embed(title=f"{res1['title']}", url=f"https://twitch.tv/{login}",
                           description=f"viewer count: `{res1['viewer_count']}`",
                           colour=discord.Colour(value=int('6441a5', 16)),
                           timestamp=datetime.now())
        em.set_author(name=f"{res1['user_name']}", url=f"https://twitch.tv/{login}",
                      icon_url=f"{res2['profile_image_url']}")
        em.set_image(url=f"{res1['thumbnail_url'].format(width=1920, height=1080)}")

        await channel.send(embed=em)

    @staticmethod
    def get_stream_query(login):
        return f"streams?user_login={login}"

    @staticmethod
    def get_user_query(login):
        return f"users?login={login}"

    @staticmethod
    def print_response(res):
        res_json = res.json()
        print_res = json.dumps(res_json, indent=2)
        return print(print_res)


class Utils:
    def __init__(self, bot):
        self.bot = bot
        self.twitch = bot.twitch
        self.main = bot.main
        self.models = bot.models
        self.config = bot.servers
        self.streamers = bot.streamers
        self.dev_key = "AIzaSyAZxekQbiyOvq1fCFNmq6-4VvNwcKQ2Vhs"
        self.dictionary = ["тыс", "млн", "млрд", "бил", "трл", "кдл", "квл", "сек", "сеп", "окт", "нон", "дец"]

    @staticmethod
    def binary_search(arr: list, item):
        n = len(arr)
        a = 0
        b = n - 1

        while a <= b:
            mid = floor((a + b) / 2)
            if arr[mid] < item:
                a = mid + 1
            elif arr[mid] > item:
                b = mid - 1
            else:
                return mid
        return None

    @staticmethod
    def bubble_sort(arr: list):
        has_swapped = True
        num_of_iterations = 0

        while has_swapped:
            has_swapped = False

            for i in range(len(arr) - num_of_iterations - 1):
                if arr[i] > arr[i + 1]:
                    arr[i], arr[i + 1] = arr[i + 1], arr[i]
                    has_swapped = True

            num_of_iterations += 1
        return arr

    @staticmethod
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

                requests.post(url=i['url'], data=data, headers=headers)

            await sleep(901)

    def uptime(self):
        start = self.main.find_one()['uptime']
        t_diff = relativedelta(datetime.now(), start)
        return '{d}d {h}h {m}m {s}s'.format(d=t_diff.days, h=t_diff.hours, m=t_diff.minutes, s=t_diff.seconds)

    def parser(self, raw: int = 0, typ: str = "numbers", start: datetime = None, end: datetime = None) -> str:
        string = f"{raw}"

        if raw > 1000:
            if typ == "numbers":
                string_raw = f"{raw:,}"
                list_raw = string_raw.split(",")

                listed = [i for i in list_raw if not string_raw.startswith(i)]
                n = [i for i in list_raw if string_raw.startswith(i)]
                end = f".{str(listed[0])[:2]}" if int(str(listed[0])[0]) > 0 else None

                string = f"{n[0]}{end or ''} {self.dictionary[len(listed) - 1]} "
        if typ == "time":
            time = relativedelta(microseconds=raw * (10 ** 6))
            if start and end:
                time = relativedelta(end, start)
            string = '{h}h {m}m {s}s'.format(h=time.hours, m=time.minutes, s=time.seconds)

        return string

    def get_info(self, video_url):
        with youtube_dl.YoutubeDL(YTDL_OPTS) as ydl:
            try:
                info = ydl.extract_info(video_url, download=False)
            except BaseException as err:
                print(err)
                info = self.get_info(video_url)

            if "_type" in info and info["_type"] == "playlist":
                info = self.get_info(info["entries"][0]['webpage_url'])

            return info

    # google api---------------------------------------------------------------------------------------------------

    def search_video(self, req: str = "×I62?564@6§85♦3◘4☻04♣") -> dict:
        os.environ["OAUTHLIB_INSECURE_TRANSPORT"] = "1"

        api_service_name = "youtube"
        api_version = "v3"

        youtube = discovery.build(
            api_service_name, api_version, developerKey=self.dev_key)

        request = youtube.videos().list(
            part="snippet,contentDetails,statistics",
            id=req,
            maxResults=1
        )
        response = request.execute()

        return response

    def search_channel(self, req: str, usid: bool = False, recurr: bool = False) -> dict:
        os.environ["OAUTHLIB_INSECURE_TRANSPORT"] = "1"

        api_service_name = "youtube"
        api_version = "v3"

        youtube = discovery.build(api_service_name, api_version, developerKey=self.dev_key)

        if usid:
            request = youtube.channels().list(part="snippet,contentDetails,statistics", id=f"{req}", maxResults=1)
            response = request.execute()

            if response['pageInfo']['totalResults'] < 1:

                if recurr:
                    raise cmd.BadArgument("Channel not found")

                return self.search_channel(req, False, True)

            return response

        request = youtube.channels().list(part="snippet,contentDetails,statistics", forUsername=f"{req}", maxResults=1)
        response = request.execute()

        if response['pageInfo']['totalResults'] < 1:

            if recurr:
                raise cmd.BadArgument("Channel not found")

            return self.search_channel(req, True, True)

        return response

    def uploader(self, track: dict = None, typ: str = "yt") -> dict:
        data = {}
        if typ == "yt":
            thumb = self.search_video(track['info']['identifier'])
            snippet = thumb['items'][0]['snippet']

            ico = self.search_channel(snippet['channelId'])
            channel_snippet = ico['items'][0]['snippet']

            img = snippet['thumbnails']
            img = snippet['thumbnails'][
                'maxres' if 'maxres' in img else 'standard' if 'standard' in img
                else 'high' if 'high' in img else 'medium' if 'medium' in img else 'default']

            icon = channel_snippet['thumbnails']
            icon = channel_snippet['thumbnails'][
                'maxres' if 'maxres' in icon else 'standard' if 'standard' in icon
                else 'high' if 'high' in icon else 'medium' if 'medium' in icon else 'default']

            data = {
                "name": channel_snippet['title'],
                "url": f"https://youtube.com/channel/{ico['items'][0]['id']}",
                "icon": icon['url'] if ico
                else "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg",
                "thumbnail": img['url'] if img
                else "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg",
                "tags": ", ".join(snippet['tags']) if "tags" in snippet else "No tags",
                "title": snippet['title'],
                "duration": self.parser(int(track['info']['length']) // 1000, typ="time"),
                "video_url": track['info']['uri']
            }
        elif typ == "sc":
            r = track['info']['uri'][8:].split("/")
            data = {
                "name": track['info']['author'],
                "url": f"https://{r[0]}/{r[1]}",
                "icon": "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg",
                "thumbnail": "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg",
                "tags": "No tags",
                "title": track['info']['title'],
                "duration": self.parser(int(track['info']['length']) // 1000, typ="time"),
                "video_url": track['info']['uri']
            }
        return data

    def now_playing(self, player) -> dict:
        if not player.current:
            player.current = player.queue[0]
        video = self.search_video(req=player.current.identifier)
        snipp = video['items'][0]['snippet']

        channel = self.search_channel(req=snipp['channelId'], usid=True)
        csnipp = channel['items'][0]['snippet']

        req = self.bot.get_guild(int(player.guild_id)).get_member(int(player.current.requester))
        img = snipp['thumbnails']
        img = snipp['thumbnails'][
            'maxres' if 'maxres' in img else 'standard' if 'standard' in img else 'high' if 'high' in img else
            'medium' if 'medium' in img else 'default']

        icon = csnipp['thumbnails']
        icon = csnipp['thumbnails'][
            'maxres' if 'maxres' in icon else 'standard' if 'standard' in icon else 'high' if 'high' in icon else
            'medium' if 'medium' in icon else 'default']
        data = {
            "img": img,
            "icon": icon,
            "req": req,
            "title": player.current.title,
            "channel": channel,
            "csnipp": csnipp,
            "tags": ", ".join(snipp['tags']) if 'tags' in snipp else "No Tags",
        }
        return data

    async def check_twitch(self):
        while 1:
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
                return await sleep(300)

            await sleep(300)

    # lavalink music -----------------------------------------------------------------------------------------------

    async def ensure_voice(self, ctx):
        """ This check ensures that the bot and command author are in the same voicechannel. """
        player = self.bot.lavalink.player_manager.create(ctx.guild.id, endpoint=str(ctx.guild.region))
        should_connect = ctx.command.name in ('Play',)

        if not ctx.author.voice or not ctx.author.voice.channel:
            raise cmd.CommandInvokeError('Join a voicechannel first.')

        if not player.is_connected:
            if not should_connect:
                raise cmd.CommandInvokeError('Not connected.')

            permissions = ctx.author.voice.channel.permissions_for(ctx.me)

            if not permissions.connect or not permissions.speak:  # Check user limit too?
                raise cmd.CommandInvokeError('I need the `CONNECT` and `SPEAK` permissions.')

            player.store('channel', ctx.channel.id)
            await self.connect_to(ctx.guild.id, str(ctx.author.voice.channel.id))
        else:
            if int(player.channel_id) != ctx.author.voice.channel.id:
                raise cmd.CommandInvokeError('You need to be in my voicechannel.')

    async def track_hook(self, event):
        if isinstance(event, lavalink.events.QueueEndEvent):
            guild_id = int(event.player.guild_id)
            cfg = self.config.find_one({"_id": guild_id})['music']

            cfg['queue'] = []
            cfg['now_playing'] = ""

            self.config.update_one({"_id": f"{guild_id}"}, {"$set": {"music": dict(cfg)}})
            
            await sleep(60)
            
            cfg = self.config.find_one({"_id": guild_id})['music']
            guild = self.bot.get_guild(int(guild_id))
            
            if cfg['now_playing'] and event.player.is_connected and len(guild.get_channel(int(player.channel_id)).members) <= 1:
                player.queue.clear()
                await player.stop()
                await self.connect_to(ctx.guild.id)
                await guild.get_channel(cfg['last']['channel']).send("End of playback, auto disconnect")

        elif isinstance(event, lavalink.TrackStartEvent):
            player = event.player
            cfg = self.config.find_one({"_id": f"{event.player.guild_id}"})['music']
            data = self.now_playing(player=player)

            if cfg['now_playing'] == "":
                cfg['now_playing'] = {}
            cfg['now_playing']['start'] = datetime.now()
            cfg['now_playing']['req'] = str(data['req'].id)
            cfg['now_playing']['title'] = data['title']
            cfg['now_playing']['tags'] = data['tags'],
            cfg['now_playing']['video_url'] = player.current.uri
            cfg['now_playing']['thumbnail'] = data['img']['url']
            cfg['now_playing']['name'] = data['csnipp']['title']
            cfg['now_playing']['icon'] = data['icon']['url']
            cfg['now_playing']['url'] = f"https://youtube.com/channel/{data['channel']['items'][0]['id']}"

            self.config.update_one({"_id": f"{player.guild_id}"}, {"$set": {"music": dict(cfg)}})

            em = discord.Embed(title=cfg['now_playing']['title'],
                               description=f"Duration: `{lavalink.format_time(int(player.current.duration))}`"
                                           f"\nTags: `{cfg['now_playing']['tags'][0]}`",
                               url=cfg['now_playing']['video_url'],
                               timestamp=datetime.now(),
                               colour=discord.Colour.green())
            em.set_image(url=cfg['now_playing']['thumbnail'])
            em.set_author(name=cfg['now_playing']['name'], url=cfg['now_playing']['url'],
                          icon_url=cfg['now_playing']['icon'])
            em.set_footer(text=f"Requested by: {str(data['req'])}",
                          icon_url=data['req'].avatar_url_as(format='png', static_format='png', size=256))

            message = await self.bot.get_guild(int(player.guild_id)).get_channel(int(cfg['last']['channel'])).send(
                embed=em)

            cfg['last'] = {"message": f"{message.id}", "channel": f"{message.channel.id}"}
            self.config.update_one({"_id": f"{player.guild_id}"}, {"$set": {"music": dict(cfg)}})

        elif isinstance(event, lavalink.TrackEndEvent):
            cfg = self.config.find_one({"_id": f"{event.player.guild_id}"})['music']
            guild = self.bot.get_guild(int(event.player.guild_id))
            
            message = await guild.get_channel(int(cfg['last']['channel'])).fetch_message(int(cfg['last']['message']))
            await message.delete()
            
            if len(guild.get_channel(int(player.channel_id)).members) <= 1:
                player.queue.clear()
                await player.stop()
                await self.connect_to(ctx.guild.id)
                
                cfg['queue'] = []
                cfg['now_playing'] = ""
                self.config.update_one({"_id": str(event.player.guild_id)}, {"$set": {"music": dict(cfg)}})
                
                return await guild.get_channel(cfg['last']['channel']).send("Empty voice channel, auto disconnect")

    async def connect_to(self, guild_id: int, channel_id: str = None):
        """ Connects to the given voicechannel ID. A channel_id of `None` means disconnect. """
        ws = self.bot._connection._get_websocket(guild_id)
        await ws.voice_state(str(guild_id), channel_id)


class Paginator:
    def __init__(self, ctx: Context, begin: Embed = None, reactions: Union[tuple, list] = None, timeout: int = 120,
                 embeds: Union[tuple, list] = None, music: dict = None, cfg=None):
        self.reactions = reactions or ('⬅', '⏹', '➡')
        self.pages = []
        self.current = 0
        self.ctx = ctx
        self.timeout = timeout
        self.begin = begin
        self.add_page(embeds)
        self.controller = None
        self.music = music
        self.cfg = cfg

    async def _close_session(self):
        await self.controller.delete()
        del self.pages
        del self.reactions
        del self.current
        del self.ctx

    def add_page(self, embeds: Union[list, tuple]):
        for i in embeds:
            if isinstance(i, Embed):
                self.pages.append(i)

    async def start(self):
        self.controller = await self.ctx.send(embed=self.begin)
        await self.controller.add_reaction(self.reactions[2])
        await self.ctx.bot.wait_for('reaction_add', timeout=self.timeout, check=lambda r, u: u.bot is not True)
        await self.call_controller()

    async def call_controller(self, start_page: int = 0):
        if start_page > len(self.pages) - 1:
            raise IndexError(f'Currently added {len(self.pages)} pages,'
                             f' but you tried to call controller with start_page = {start_page}')
        if not self.controller:
            self.controller = await self.ctx.send(embed=self.pages[start_page])
        else:
            await self.controller.edit(embed=self.pages[start_page])

        try:
            await self.controller.clear_reactions()
        except BaseException as err:
            print(err)
            pass

        try:
            for emoji in self.reactions:
                await self.controller.add_reaction(emoji)
        except BaseException as err:
            print(err)
            pass

        while True:
            try:
                def check(r, u) -> bool:
                    return u.id == self.ctx.author.id \
                                         and r.emoji in self.reactions \
                                         and r.message.id == self.controller.id
                response = await self.ctx.bot.wait_for('reaction_add', timeout=self.timeout,
                                                       check=check)
            except TimeoutError:
                break

            try:
                await self.controller.remove_reaction(response[0], response[1])
            except BaseException as err:
                print(err)
                pass

            if response[0].emoji == self.reactions[0]:
                self.current = self.current - 1 if self.current > 0 else len(self.pages) - 1
                await self.controller.edit(embed=self.pages[self.current])

            if response[0].emoji == self.reactions[1]:
                # em = self.controller.embeds[0]
                # if em.title == "Queue list:":
                #     self.music['queue'] = []
                #     self.cfg.update_one({"_id": f"{self.ctx.guild.id}"}, {"$set": {"music": dict(self.music)}})
                break

            if response[0].emoji == self.reactions[2]:
                self.current = self.current + 1 if self.current < len(self.pages) - 1 else 0
                await self.controller.edit(embed=self.pages[self.current])

        await self._close_session()


class DataBase:
    def __init__(self, bot):
        self.bot = bot
        self.models = bot.models
        self.servers = bot.servers
        self.profiles = bot.profiles

    async def create(self):
        await self._create_servers()
        print(f"created: servers")
        await self._create_users()
        print(f"created: users")

    async def _create_servers(self):
        arr = [int(i['_id']) for i in self.servers.find()]

        create = [i for i in self.bot.guilds if i.id not in arr]
        for i in create:
            self.servers.insert_one(self.models.Server(i).get_dict())
            print(f"created: {i.id}")

    async def _create_users(self):
        arr = [f"{i['sid']} {i['uid']}" for i in self.profiles.find()]

        raw = [i.members for i in self.bot.guilds]
        create = [i for x in raw for i in x if f"{i.guild.id} {i.id}" not in arr]
        for i in create:
            mem = self.models.User(i).get_dict()
            self.profiles.insert_one(mem)
            print(f"created: {mem['sid']} | {mem['uid']}")

    async def create_server(self, guild: discord.Guild):
        i = self.models.Server(guild).get_dict()
        srv = self.servers.find_one({"_id": f"{i['_id']}"})

        if not srv:
            self.servers.insert_one(i)
            print(f"created: {i['_id']}")
        for i in guild.members:
            await self.create_user(i)

    async def create_user(self, member: discord.Member):
        i = self.models.User(member).get_dict()
        usr = self.servers.find_one({"sid": f"{i['sid']}", "uid": f"{i['uid']}"})

        if not usr:
            self.profiles.insert_one(i)
            print(f"created: {i['sid']} | {i['uid']}")


class EmbedGenerator:
    def __init__(self, target: str, inp: dict = None, **kwargs):
        if not inp:
            inp = {}
        for name, value in kwargs.items():
            inp[str(name)] = value

        if target == "queue":
            video = inp['video']
            ctx = inp['ctx']
            em = video.get_embed()

            embed = Embed(title=video.title,
                          url=video.video_url,
                          colour=Colour.green())

            embed.set_thumbnail(url=em.image.url)
            embed.set_author(name=f"{str(ctx.author)} add to queue:",
                             icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))
            self.embed = embed

        elif target == "bot":
            system = inp['system']
            cpu = inp['cpu']
            ram = inp['ram']
            platform = inp['platform']
            ctx = inp['ctx']
            data = inp['data']

            em = discord.Embed(title=f"{data.arrowl} {ctx.me.name} {data.bot.version} info {data.arrowr}",
                               colour=discord.Colour.green(),
                               timestamp=datetime.now())
            em.add_field(name="OS:", value=f"`{system[0]} {system[2]}`")
            em.add_field(name="CPU:", value=f'`{cpu}`')
            em.add_field(name="RAM:", value=ram)
            em.add_field(name="Users:", value=f"`{len(data.bot.users)}`")
            em.add_field(name="Guilds:", value=f"`{len(data.bot.guilds)}`")
            em.add_field(name='\u200b', value="\u200b")
            em.add_field(name="Up-time:", value=f"`{data.utils.uptime()}`")
            em.add_field(name="Ping:", value=f"`{round(data.bot.latency * 1000, 1)}s`")
            em.add_field(name='\u200b', value="\u200b")
            em.add_field(name="Python version:", value=f"`{platform.python_version()}`")
            em.add_field(name="Discord.Py version:",
                         value=f"`{discord.version_info[0]}.{discord.version_info[1]}.{discord.version_info[2]}`")
            em.add_field(name='\u200b', value="\u200b")
            em.add_field(name="Bot invite:",
                         value=f"[Click]({data.bot_invite.format(id=data.bot.user.id, perms=536210647)})")
            em.add_field(name="Support server:", value=f"[Click]({data.supp_link})")
            em.add_field(name="Patreon:", value=f"[Click]({data.patreon_link})")

            em.set_footer(text=str(ctx.author),
                          icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

            self.embed = em

    def get(self) -> Embed:
        return self.embed
