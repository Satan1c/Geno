# -*- coding: utf-8 -*-

import os
from asyncio import TimeoutError
from datetime import datetime
from time import strftime
from typing import Union

import youtube_dl
from dateutil.relativedelta import relativedelta
from discord import Embed, VoiceClient, User, Colour, AudioSource
from discord.ext import commands as cmd
from discord.ext.commands import Context
from googleapiclient import discovery
import json
import asyncio
import discord

YTDL_OPTS = {
    "default_search": "ytsearch",
    "format": "bestaudio/best",
    "quiet": True,
    "no_warnings": True,
    "extract_flat": "in_queue",
    "ignoreerrors": True,
}


class Video:
    def __init__(self, url_or_search: str, requested_by: User, utils):
        self.utils = utils
        with youtube_dl.YoutubeDL(YTDL_OPTS):
            video = self._get_info(url_or_search)
            video_format = video["formats"][0]
            self.stream_url = video_format["url"]
            self.video_url = video["webpage_url"]
            self.title = str(video["title"])
            self.uploader = utils.uploader(video)
            self.thumbnail = video["thumbnail"] if "thumbnail" in video else None
            self.duration = video['duration']
            self.req = {"id": str(requested_by.id), "tag": str(requested_by), "ava": requested_by.avatar_url_as(format="png", static_format='png', size=256)}

    def _get_info(self, video_url):
        with youtube_dl.YoutubeDL(YTDL_OPTS) as ydl:
            try:
                info = ydl.extract_info(video_url, download=False)
            except:
                info = self._get_info(video_url)

            if "_type" in info and info["_type"] == "playlist":
                    info = self._get_info(info["entries"][0]['webpage_url'])

            return info

    def get_embed(self) -> Embed:
        embed = Embed(title=self.title,
                              description=f"Duration is: `{self.utils.parser(self.duration, 'time')}`"
                              f"\nVideo tags: `{self.uploader['tags']}`",
                              url=self.video_url,
                              colour=Colour.green(),
                              timestamp=datetime.now())

        embed.set_footer(text=f"Requested by {self.req['tag']}", icon_url=self.req['ava'])
        embed.set_author(name=self.uploader['name'], url=self.uploader['url'], icon_url=self.uploader['icon'])

        if self.thumbnail:
            embed.set_image(url=self.uploader['thumbnail'])

        return embed


class Utils:
    def __init__(self, bot):
        self.bot = bot
        self.main = bot.main
        self.models = bot.models
        self.config = bot.servers
        self.dev_key = "AIzaSyAZxekQbiyOvq1fCFNmq6-4VvNwcKQ2Vhs"
        self.dictionary = ["тыс", "млн", "млрд", "бил", "трл", "кдл", "квл", "сек", "сеп", "окт", "нон", "дец"]

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
            time = relativedelta(microseconds=raw*(10**6))
            if start and end:
                time = relativedelta(end, start)
            string = '{h}h {m}m {s}s'.format(h=time.hours, m=time.minutes, s=time.seconds)

        return string

    # google api------------------------------------------------------------------------------------------------------------

    def search_video(self, req: str = "×I62?564@6§85♦3◘4☻04♣"):
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

    def search_channel(self, req: str, usid: bool = False, recurr: bool = False):
        os.environ["OAUTHLIB_INSECURE_TRANSPORT"] = "1"

        api_service_name = "youtube"
        api_version = "v3"

        youtube = discovery.build(api_service_name, api_version, developerKey=self.dev_key)

        if usid:
            request = youtube.channels().list(part="snippet,contentDetails,statistics", id=f"{req}", maxResults=1)
            response = request.execute()

            if response['pageInfo']['totalResults'] < 1:

                if recurr:
                    return None

                return self.search_channel(req, False, True)

            return response

        request = youtube.channels().list(part="snippet,contentDetails,statistics", forUsername=f"{req}", maxResults=1)
        response = request.execute()

        if response['pageInfo']['totalResults'] < 1:

            if recurr:
                return None

            return self.search_channel(req, True, True)

        return response

    def uploader(self, video=None) -> dict:
        ico = self.search_channel(video['uploader_id'])
        thumb = self.search_video(video['id'])
        snipp = thumb['items'][0]['snippet']
        img = snipp['thumbnails']
        img = snipp['thumbnails']['maxres' if 'maxres' in img else 'standard' if 'standard' in img else 'high' if 'high' in img else 'medium' if 'medium' in img else 'default']
        data = {
            "name": video['uploader'],
            "url": video['channel_url'],
            "icon": ico['items'][0]['snippet']['thumbnails']['high']['url'] if ico \
                else "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg",
            "thumbnail": img['url'],
            "tags": ", ".join(snipp['tags']) if "tags" in snipp else "No tags"
        }
        return data

    # _music----------------------------------------------------------------------------------------------

    def play(self, url: str, cfg: dict, ctx: cmd.Context = None, req: User = None):
        if not req:
            req = ctx.author

        video = Video(url, req, self)

        source = discord.PCMVolumeTransformer(
            discord.FFmpegPCMAudio(video.stream_url,
                                   before_options="-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5",
                                   options='-vn'), volume=cfg['volume'])

        return source, video
    
    async def queue(self, ctx: cmd.Context, music: dict, video: Video):
        data = self.models.Queue(video).get_dict()
        music['queue'].append(data)

        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})

        em = EmbedGenerator(target="queue", video=video, ctx=ctx).get()
        m = await ctx.send(embed=em)
        await m.delete(delay=120)
    
    def after(self, ctx: cmd.Context, client: VoiceClient, err, after_playing):
        if err:
            raise err
        music = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']

        if len(music['queue']) < 1:
            music['now_playing'] = ""
            self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})

            client.stop()
            asyncio.run_coroutine_threadsafe(client.disconnect(), self.bot.loop)

        else:
            q = music['queue'].pop(0)
            source, video = self.play(url=q['url'], req=self.bot.get_user(int(q['req'])), cfg=music)
            music['now_playing'] = self.models.NowPlaying(video).get_dict()

            np = str(type(music['now_playing']['title']))
            if np in ["<class 'tuple'>", "<class 'list'>"]:
                music['now_playing']['title'] = music['now_playing']['title'][0]

            self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})
            asyncio.run_coroutine_threadsafe(ctx.send(embed=video.get_embed()), self.bot.loop)

            client.play(source, after=after_playing)
    
    async def play_check(self, ctx: cmd.Context, url: str, client: VoiceClient):
        if not url:
                return "Please give video url or title"
        if not ctx.author.voice or not ctx.author.voice.channel:
                return "To use this command: you must be in voice channel, or check bot permissions to view it"
        if not client or not client.channel:
                client = await ctx.author.voice.channel.connect()
                return False
    
    async def volume_check(self):
        pass

class Paginator:
    def __init__(self, ctx: Context, begin: Embed = None, reactions: Union[tuple, list] = None, timeout: int = 120,
                 embeds: Union[tuple, list] = None, music: dict = None, cfg = None):
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
        await self.ctx.bot.wait_for('reaction_add', timeout=self.timeout, check=lambda r, u: u.bot != True)
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
        except:
            pass
        
        for emoji in self.reactions:
            await self.controller.add_reaction(emoji)

        while True:
            try:
                response = await self.ctx.bot.wait_for('reaction_add', timeout=self.timeout,
                                                       check=lambda r, u: u.id == self.ctx.author.id
                                                                          and r.emoji in self.reactions \
                                                                          and r.message.id == self.controller.id)
            except TimeoutError:
                break

            if not isinstance(self.ctx.channel, discord.DMChannel):
                try:
                    await self.controller.remove_reaction(response[0], response[1])
                except:
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
        guilds = [self.models.Server(i).get_dict() for i in self.bot.guilds]

        for i in guilds:
            srv = self.servers.find_one({"_id": f"{i['_id']}"})

            if not srv:
                self.servers.insert_one(i)
                print(f"created: {i['_id']}")

    async def _create_users(self):
        raw = [i.members for i in self.bot.guilds]

        for x in raw:
            for i in x:
                prf = self.profiles.find_one({"sid": f"{i.guild.id}", "uid": f"{i.id}"})

                if not prf:
                    mem = self.models.User(i).get_dict()
                    self.profiles.insert_one(mem)
                    print(f"created: {mem['sid']} | {mem['uid']}")
    
    async def create_server(self, guild: discord.Guild):
        i = self.models.Server(guild).get_dict()
        srv = self.servers.find_one({"_id": f"{i['_id']}"})

        if not srv:
            self.servers.insert_one(i)
            print(f"created: {i['_id']}")
    
    async def create_user(self, member: discord.Member):
        i = self.models.User(i).get_dict()
        usr = self.servers.find_one({"sid": f"{i['sid']}", "uid": f"{i['uid']}"})

        if not usr:
            self.profiles.insert_one(i)
            print(f"created: {i['sid']} | {i['uid']}")

class EmbedGenerator:
    def __init__(self, target: str, inp: dict = {}, **kwargs):
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
            em.add_field(name="Ping:", value=f"`{round(data.bot.latency*1000, 1)}s`")
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
