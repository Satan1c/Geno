# -*- coding: utf-8 -*-

import os
from asyncio import TimeoutError
from datetime import datetime
from time import strftime
from typing import Union

import discord
import youtube_dl
from dateutil.relativedelta import relativedelta
from discord import Embed
from discord.ext import commands as cmd
from discord.ext.commands import Context
from googleapiclient import discovery
import json

YTDL_OPTS = {
    "default_search": "ytsearch",
    "format": "bestaudio/best",
    "quiet": True,
    "extract_flat": "in_queue"
}


class Video:
    def __init__(self, url_or_search: str, requested_by: discord.User, utils):
        with youtube_dl.YoutubeDL(YTDL_OPTS):
            video = self._get_info(url_or_search)
            video_format = video["formats"][0]
            self.stream_url = video_format["url"]
            self.video_url = video["webpage_url"]
            self.title = video["title"]
            self.uploader = utils.uploader(video)
            self.thumbnail = video["thumbnail"] if "thumbnail" in video else None
            self.duration = utils.parser(video['duration'], "time")
            self.req = {"tag": str(requested_by), "ava": requested_by.avatar_url_as(format="png", static_format='png', size=256)}

    def _get_info(self, video_url):
        with youtube_dl.YoutubeDL(YTDL_OPTS) as ydl:
            info = ydl.extract_info(video_url, download=False)

            if "_type" in info and info["_type"] == "playlist":
                return self._get_info(info["entries"][0]['webpage_url'])

            return info

    def get_embed(self) -> discord.Embed:
        embed = discord.Embed(title=self.title,
                              description=f"Duration is: `{self.duration}`"
                              f"\nVideo tags: `{self.uploader['tags']}`",
                              url=self.video_url,
                              colour=discord.Colour.green(),
                              timestamp=datetime.now())

        embed.set_footer(text=f"Requested by {self.req['tag']}", icon_url=self.req['ava'])
        embed.set_author(name=self.uploader['name'], url=self.uploader['url'], icon_url=self.uploader['icon'])

        if self.thumbnail:
            embed.set_image(url=self.uploader['thumbnail'])

        return embed


class Utils:
    def __init__(self, bot):
        self.bot = bot
        self.dev_key = "AIzaSyAZxekQbiyOvq1fCFNmq6-4VvNwcKQ2Vhs"
        self.dictionary = ["тыс", "млн", "млрд", "бил", "трл", "кдл", "квл", "сек", "сеп", "окт", "нон", "дец"]

    def uptime(self, start, now):
        t_diff = relativedelta(now, start)
        return '{d}d {h}h {m}m {s}s'.format(d=t_diff.days, h=t_diff.hours, m=t_diff.minutes, s=t_diff.seconds)

    def parser(self, raw: int = 0, typ: str = "numbers") -> str:
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
            string = '{h}h {m}m {s}s'.format(h=time.hours, m=time.minutes, s=time.seconds)

        return string

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

    def play(self, url: str, ctx: cmd.Context = None, req: discord.User = None):
        if not req:
            req = self.bot.get_user(ctx.author.id)

        video = Video(url, req, self)

        source = discord.PCMVolumeTransformer(
            discord.FFmpegPCMAudio(video.stream_url,
                                   before_options="-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5",
                                   options='-vn'), volume=0.5)

        return source, video


class Paginator:
    def __init__(self, ctx: Context, begin: Embed, reactions: Union[tuple, list] = None, timeout: int = 120,
                 embeds: Union[tuple, list] = None):
        self.reactions = reactions or ('⬅', '⏹', '➡')
        self.pages = []
        self.current = 0
        self.ctx = ctx
        self.timeout = timeout
        self.begin = begin
        self.add_page(embeds)

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

        await self.controller.edit(embed=self.pages[start_page])
        if not isinstance(self.ctx.channel, discord.DMChannel):
            await self.controller.clear_reactions()
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
