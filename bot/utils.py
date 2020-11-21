# -*- coding: utf-8 -*-

import asyncio
import json
import os
import re
import urllib
from asyncio import TimeoutError
from datetime import datetime
from math import floor
from typing import Union, Tuple

import requests
from dateutil.relativedelta import relativedelta
from googleapiclient import discovery

import discord
from discord import Embed, Colour
from discord.ext import commands as cmd
from discord.ext.commands import Context

url_rx = re.compile(r'https?://(?:www\.)?.+')
YTDL_OPTS = {
    "default_search": "ytsearch",
    "format": "bestaudio/best",
    "quiet": True,
    "no_warnings": True,
    "extract_flat": "in_queue",
    "ignoreerrors": True,
    "forceurl": True,
    "simulate": True
    }


class Twitch:
    def __init__(self):
        self.client_id = "zoucwv8yjggvgydciwu6vluq20c533"  # https://dev.twitch.tv/console/apps
        self.token = "4bov40pb2i9d3agv7mltnybiejdu4y"  # https://twitchapps.com/tokengen/  https://dev.twitch.tv/
        self.url = "https://api.twitch.tv/helix/"

    def get_response(self, query):
        res = requests.get(f"{self.url}{query}",
                           headers={
                               "Client-ID": self.client_id, 'Authorization': f'Bearer {self.token}',
                               "Accept": "application/vnd.v5+json"
                               })
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

    def uptime(self):
        start = self.main.find_one()['uptime']
        t = relativedelta(datetime.now(), start)
        return '{d}d {h}h {m}m {s}s'.format(d=t.days,
                                            h=t.hours,
                                            m=t.minutes,
                                            s=t.seconds)

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
            t = relativedelta(microseconds=raw * (10 ** 6))
            if start and end:
                t = relativedelta(end, start)
            string = '{h}:{m}:{s}'.format(h=t.hours if len(f"{t.hours}") >= 2 else f"0{t.hours}",
                                          m=t.minutes if len(f"{t.minutes}") >= 2 else f"0{t.minutes}",
                                          s=t.seconds if len(f"{t.seconds}") >= 2 else f"0{t.seconds}")

        return string

    def rroles_check(self, roles: list, ctx: cmd.Context) -> Tuple[bool, Union[list, None]]:
        if len(roles) > 0 and isinstance(roles[0], list):
            res = []
            for i in roles:
                f, s = self.rroles_check(i[1:], ctx)
                s.insert(0, i[0])
                res.append(s)
            return True, res
        for r in roles:
            try:
                role = ctx.guild.get_role(int(r))

            except BaseException as err:
                print("[!] rroles_check:", err)
                break

            if not role:
                break
        else:
            return True, roles

        return False, None

    async def get_info(self, video_url: str, max_results: int = 1) -> str:
        try:
            video_url = urllib.parse.quote(video_url)
            info = json.loads(requests.get(f"https://geno.glitch.me/youtube/{video_url}").text)
            info = "https://www.youtube.com" + info[0]['url_suffix'] if info and len(info) else None

            if not info:
                return video_url

            return info
        except:
            print("fail 2")

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

            if response['pageInfo']['resultsPerPage'] < 1:

                if recurr:
                    raise cmd.BadArgument("Channel not found")

                return self.search_channel(req, False, True)

            return response

        request = youtube.channels().list(part="snippet,contentDetails,statistics", forUsername=f"{req}", maxResults=1)
        response = request.execute()

        if response['pageInfo']['resultsPerPage'] < 1:

            if recurr:
                raise cmd.BadArgument("Channel not found")

            return self.search_channel(req, True, True)

        return response

    def uploader(self, track: dict = None, typ: str = "yt") -> dict:
        if typ == "yt":
            thumb = self.search_video(track['info']['identifier'])
            snippet = thumb['items'][0]['snippet']

            ico = self.search_channel(snippet['channelId'], usid=True)
            channel_snippet = ico['items'][0]['snippet']

            img = snippet['thumbnails']
            img = snippet['thumbnails'][
                'maxres' if 'maxres' in img else 'standard' if 'standard' in img
                else 'high' if 'high' in img else 'medium' if 'medium' in img else 'default']

            icon = channel_snippet['thumbnails']
            icon = channel_snippet['thumbnails'][
                'maxres' if 'maxres' in icon else 'standard' if 'standard' in icon
                else 'high' if 'high' in icon else 'medium' if 'medium' in icon else 'default']

        elif typ == "sc":
            r = track['info']['uri'][8:].split("/")

        return {
            "name": channel_snippet['title'],
            "url": f"https://youtube.com/channel/{ico['items'][0]['id']}",
            "icon": icon[
                'url'] if ico else "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg",
            "thumbnail": img[
                'url'] if img else "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg",
            "tags": ", ".join(snippet['tags']) if "tags" in snippet else "No tags",
            "title": snippet['title'],
            "duration": self.parser(int(track['info']['length']) // 1000, typ="time"),
            "video_url": track['info']['uri']

            } if typ == "yt" else {

            "name": track['info']['author'],
            "url": f"https://{r[0]}/{r[1]}",
            "icon": "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg",
            "thumbnail": "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg",
            "tags": "No tags",
            "title": track['info']['title'],
            "duration": self.parser(int(track['info']['length']) // 1000, typ="time"),
            "video_url": track['info']['uri']
            }

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

        return {
            "img": img,
            "icon": icon,
            "req": req,
            "title": player.current.title,
            "channel": channel,
            "csnipp": csnipp,
            "tags": ", ".join(snipp['tags']) if 'tags' in snipp else "No Tags",
            }

    async def queue(self, ctx: cmd.Context, player) -> list:
        data = self.now_playing(player)

        em = discord.Embed(title="Seems like queue is empty",
                           colour=discord.Colour.green(),
                           timestamp=datetime.now())
        em.set_author(name="Seems like nothing is plying now",
                      icon_url="https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg")

        em.set_thumbnail(url="https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg")

        if len(player.queue):
            embeds = []
            desc = []
            for i in range(len(player.queue)):
                n = i + 1
                j = player.queue[i]
                mem = ctx.guild.get_member(int(j.requester))
                desc.append(f"`{n}` Requested by: {str(mem or 'User not found')}[{mem.mention if mem else ''}]"
                            f"\n[{j.title}](https://www.youtube.com/watch?v={j.identifier})")

                if n % 5 == 0:
                    embeds.append(discord.Embed(title=f"{player.current.title}"
                                                      f"\nQueue list:",
                                                url=player.current.uri,
                                                colour=discord.Colour.green(),
                                                timestamp=datetime.now(),
                                                description="\n".join(desc[len(embeds) * 5:]))
                                  .set_thumbnail(url=data['img']['url'])
                                  .set_author(name=data['csnipp']['title'],
                                              url=f"https://youtube.com/channel/"
                                                  f"{data['channel']['items'][0]['id']}",
                                              icon_url=data['icon']['url'])
                                  .set_footer(text=str(ctx.author),
                                              icon_url=ctx.author.avatar_url_as(format="png",
                                                                                static_format='png',
                                                                                size=256)))
            else:
                if len(desc[len(embeds) * 5:]) > 0:
                    try:
                        embeds.append(discord.Embed(title=f"{player.current.title}"
                                                          f"\nQueue list:",
                                                    url=player.current.uri,
                                                    colour=discord.Colour.green(),
                                                    timestamp=datetime.now(),
                                                    description="\n".join(desc[len(embeds) * 5:]))
                                      .set_thumbnail(url=data['img']['url'])
                                      .set_author(name=data['csnipp']['title'],
                                                  url=f"https://youtube.com/channel/"
                                                      f"{data['channel']['items'][0]['id']}",
                                                  icon_url=data['icon']['url'])
                                      .set_footer(text=str(ctx.author),
                                                  icon_url=ctx.author.avatar_url_as(format="png",
                                                                                    static_format='png',
                                                                                    size=256)))
                    except BaseException as err:
                        print(err)
                        pass

            if len(embeds):
                return embeds

        if data:
            em.set_thumbnail(url=data['img']['url'])
            em.set_author(name=data['csnipp']['title'],
                          url=f"https://youtube.com/channel/{data['channel']['items'][0]['id']}",
                          icon_url=data['icon']['url'])
            em.title = player.current.title
            em.url = player.current.uri
            em.set_footer(text=str(ctx.author),
                          icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

        return [em]

    @staticmethod
    async def volume(value=None, raw: int = 0, ctx: cmd.Context = None):
        em = discord.Embed(title="Volume change", description="From: `{raw}%`\nTo: `{end}%`",
                           colour=discord.Colour.green(), timestamp=datetime.now())
        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

        if not value:
            em.title = "Volume"
            em.description = f"`{int(raw)}%`"
            await ctx.send(embed=em)
            return None, None

        try:
            value = int(value)
        except BaseException as err:
            print(err)
            r = re.sub(r'[^.0-9]', r'', value)
            value = round(float(r)) if r else None

        if not value:
            em.title = "Volume"
            em.description = f"`{int(raw)}%`"
            await ctx.send(embed=em)
            return None, None

        if value < 1:
            value = 1
            await ctx.send(embed=discord.Embed(description=f"Volume value can't be less than {value}%"))
        elif value > 250:
            value = 250
            await ctx.send(embed=discord.Embed(description=f"Volume value can't be more than {value}"))
        if value == raw:
            await ctx.send(embed=discord.Embed(description="New volume value can't equals to old"))
            return None, None

        return em, value

    async def reaction_roles(self, ctx: cmd.Context, message: str, args: tuple) -> Tuple[list, list, discord.Message]:
        if len(re.sub(r"[^0-9]", r"", f"{message}")) == 18:
            for i in ctx.guild.text_channels:
                print(message)
                try:
                    message = await i.fetch_message(int(re.sub(r"[^0-9]", r"", f"{message}")))
                    if message:
                        break
                except BaseException as err:
                    print(err)
                    continue

        key = args[::2]
        k = []
        for i in key:
            print(i)
            ids = re.sub(r"[^0-9]", r"", f"{i}")
            if len(ids) == 18:
                e = self.bot.get_emoji(int(ids))
                if not e:
                    k.append(i)
                    continue
                k.append(e.id)
            else:
                k.append(i)
        key = k

        value = args[1::2]
        if len(ctx.message.role_mentions) > 0 and len(ctx.message.role_mentions) == len(value):
            value = ctx.message.roles_mentions
        else:
            v = []
            m = 0
            for i in range(len(value)):
                if len(re.sub(r"[^0-9]", r"", f"{value[i]}")) == 18:
                    r = ctx.guild.get_role(int(re.sub(r"[^0-9]", r"", f"{value[i]}")))
                    if not r:
                        raise cmd.BadArgument("Role not found")
                    v.append(r)
                elif len(ctx.message.role_mentions) > 0:
                    r = ctx.guild.get_role(ctx.message.role_mentions[m].id)
                    if not r:
                        raise cmd.BadArgument("Role not found")
                    m += 1
                    v.append(r)
            else:
                value = v

        return key, value, message

    @staticmethod
    async def twitch_nickname(ctx: cmd.Context, nick: str, channel: str) -> Tuple[str, discord.TextChannel]:
        if len(ctx.message.channel_mentions) > 0:
            channel = ctx.guild.get_channel(int(ctx.message.channel_mentions[0].id))
        elif len(re.sub(r"[^0-9]", r"", f"{channel}")) == 18:
            channel = ctx.guild.get_channel(int(re.sub(r"[^0-9]", r"", channel)))

        if not nick and channel and channel not in ctx.guild.channels:
            nick = channel
            channel = ctx.channel
        elif not channel and not nick:
            channel = ctx.channel
            nick = "satan1clive"
        elif not channel and nick:
            channel = ctx.channel
        elif not nick:
            nick = "satan1clive"
        elif not channel:
            channel = ctx.channel
        if nick and url_rx.match(nick):
            nick = nick.split("/")[-1]

        return nick, channel


class Paginator:
    def __init__(self, ctx: Context, begin: Embed = None, reactions: Union[tuple, list] = None, timeout: int = 120,
                 embeds: Union[tuple, list] = None):
        self.reactions = reactions or ('⬅', '⏹', '➡')
        self.pages = []
        self.current = 0
        self.ctx = ctx
        self.timeout = timeout
        self.begin = begin
        self.add_page(embeds)
        self.controller = None

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
        await asyncio.gather(self._create_servers())  # , self._create_users())

    async def _create_servers(self):
        arr = [int(i['_id']) for i in self.servers.find()]

        create = [self.models.Server(i).get_dict() for i in self.bot.guilds if i.id not in arr]
        if len(create):
            print("...servers creation")
            self.servers.insert_many(create)

        del create, arr
        print("created: servers")

    async def __create_users(self):
        arr = [f"{i['sid']} {i['uid']}" for i in self.profiles.find()]

        raw = [[x for x in i.members if not x.bot] for i in self.bot.guilds]
        create = [self.models.User(i).get_dict() for x in raw for i in x if f"{i.guild.id} {i.id}" not in arr]

        if len(create):
            print("...users creation")
            self.profiles.insert_many(create)

        del create, raw, arr
        print("created: users")

    async def _create_users(self):
        arr = [f"{i['sid']} {i['uid']}" for i in self.profiles.find()]
        raw = [[f"{member.guild.id} {member.id}", member] for guild in self.bot.guilds for member in guild.members if
               not member.bot]
        if len(arr) == len(raw):
            return
        create = [i[1] for i in raw if i[0] not in arr]
        create = self.models.User.bulk_create(create)

        if len(create):
            print("...users creation")
            self.profiles.insert_many(create)

        del create, arr
        print("created: users")

    async def create_server(self, guild: discord.Guild):
        i = self.models.Server(guild).get_dict()
        srv = self.servers.find_one({"_id": f"{i['_id']}"})

        if not srv:
            self.servers.insert_one(i)
            print(f"created: {i['_id']}")

        for i in guild.members:
            await self.create_user(i)

        del srv, i

    async def create_user(self, member: discord.Member):
        if member.bot:
            return
        i = self.models.User(member).get_dict()
        usr = self.servers.find_one({"sid": f"{i['sid']}", "uid": f"{i['uid']}"})

        if not usr:
            self.profiles.insert_one(i)
            print(f"created: {i['sid']} | {i['uid']}")

        del i, usr


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

            em = discord.Embed(title=f"{ctx.me.name} {data.bot.version} info",
                               colour=discord.Colour.green(),
                               timestamp=datetime.now())
            em.add_field(name="OS:", value=f"`{system[0]} {system[2]}`")
            em.add_field(name="CPU:", value=f'`{cpu}`')
            em.add_field(name="RAM:", value=ram)
            em.add_field(name="Users:", value=f"`{len([i.id for i in data.bot.users if not i.bot])}`")
            em.add_field(name="Guilds:", value=f"`{len(data.bot.guilds)}`")
            em.add_field(name='\u200b', value="\u200b")
            em.add_field(name="Up-time:", value=f"`{data.utils.uptime()}`")
            em.add_field(name="Ping:", value=f"`{round(data.bot.latency * 1000, 1)}s`")
            em.add_field(name='\u200b', value="\u200b")
            em.add_field(name="Python version:", value=f"`{platform.python_version()}`")
            em.add_field(name="Discord.Py version:",
                         value=f"`{discord.version_info[0]}.{discord.version_info[1]}.{discord.version_info[2]}`")
            em.add_field(name='\u200b', value="\u200b")

            em.set_footer(text=str(ctx.author),
                          icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

            self.embed = em

    def get(self) -> Embed:
        return self.embed


class Checks:
    def __init__(self, bot):
        self.cmds = bot.cmds
        self.bot = bot

    async def is_off(self, ctx: cmd.Context) -> bool:
        if ctx.author.id == self.bot.owner_id:
            return True

        if not ctx.guild:
            return True

        cfg = self.cmds.find_one({"_id": f"{ctx.guild.id}"})
        if not cfg:
            return True

        if ctx.command.name in cfg['commands'] or ctx.command.cog_name in cfg['cogs']:
            raise cmd.BadArgument("Module or command, is disabled on server")

        return True
