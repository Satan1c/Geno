# -*- coding: utf-8 -*-
import asyncio
import json
import os
import re
import urllib
from datetime import datetime
from typing import Union, Tuple, List

import aiohttp
from dateutil.relativedelta import relativedelta
from googleapiclient import discovery
from pymongo.collection import Collection

import discord
from discord.ext import commands as cmd

url_rx = re.compile(r'https?://(?:www\.)?.+')


class Utils:
    def __init__(self, bot):
        self.bot = bot
        self.models = bot.models
        self.config: Collection = bot.servers
        self.streamers: Collection = bot.streamers
        self.dev_key: str = bot.raw_main['dev_key']
        self.dictionary = ["тыс", "млн", "млрд", "бил", "трл", "кдл", "квл", "сек", "сеп", "окт", "нон", "дец"]

    async def uptime(self):
        start = await self.bot.main_cfg.find_one()
        start = start['uptime']
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
                print(f"\n[!]Utils rroles_check error:\n{err}")
                break

            if not role:
                break
        else:
            return True, roles

        return False, None

    def uploader(self, track: dict = None, typ: str = "yt") -> dict:
        if typ == "yt":
            thumb = self.search_video(track['info']['identifier'])
            snippet: dict = thumb['items'][0]['snippet']

            ico = self.search_channel(snippet['channelId'], usid=True)
            channel_snippet: dict = ico['items'][0]['snippet']

            img: dict = snippet['thumbnails']
            img = snippet['thumbnails'][
                'maxres' if 'maxres' in img else 'standard' if 'standard' in img
                else 'high' if 'high' in img else 'medium' if 'medium' in img else 'default']

            icon: dict = channel_snippet['thumbnails']
            icon = channel_snippet['thumbnails'][
                'maxres' if 'maxres' in icon else 'standard' if 'standard' in icon
                else 'high' if 'high' in icon else 'medium' if 'medium' in icon else 'default']

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

            }

        else:  # typ == "sc":
            r: list = track['info']['uri'][8:].split("/")
            return {

                "name": track['info']['author'],
                "url": f"https://{r[0]}/{r[1]}",
                "icon": "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg",
                "thumbnail": "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg",
                "tags": "Нет тэгов",
                "title": track['info']['title'],
                "duration": self.parser(int(track['info']['length']) // 1000, typ="time"),
                "video_url": track['info']['uri']
            }

    def now_playing(self, player) -> dict:
        if not player.current:
            player.current = player.queue[0]
        video = self.search_video(req=player.current.identifier)
        snipp: dict = video['items'][0]['snippet']

        channel = self.search_channel(req=snipp['channelId'], usid=True)
        csnipp = channel['items'][0]['snippet']

        req = self.bot.get_guild(int(player.guild_id)).get_member(int(player.current.requester))
        img: dict = snipp['thumbnails']
        img = snipp['thumbnails'][
            'maxres' if 'maxres' in img else 'standard' if 'standard' in img else 'high' if 'high' in img else
            'medium' if 'medium' in img else 'default']

        icon: dict = csnipp['thumbnails']
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

    async def queue(self, ctx: cmd.Context, player) -> List[discord.Embed]:
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
                desc.append(
                    f"`{n}` добавил в очередь: {str(mem or 'Пользователь не найден')}[{mem.mention if mem else ''}]"
                    f"\n[{j.title}](https://www.youtube.com/watch?v={j.identifier})")

                if n % 5 == 0:
                    embeds.append(discord.Embed(title=f"{player.current.title}"
                                                      f"\nСписок очереди:",
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
                                                          f"\nСписок очереди:",
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
                        print("\n", "-" * 30, f"\n[!]Utils queue error:\n{err}\n", "-" * 30, "\n")
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
    async def volume(value: Union[int, float] = None, raw: int = 0, ctx: cmd.Context = None) -> Union[
        Tuple[discord.Embed, int], Tuple[None, None]]:
        em = discord.Embed(title="Изменение громкости", description="С: `{raw}%`\nНа: `{end}%`",
                           colour=discord.Colour.green(), timestamp=datetime.now())
        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

        if not value:
            em.title = "Громкость"
            em.description = f"`{int(raw)}%`"
            await ctx.send(embed=em)
            return None, None

        try:
            value = int(value)
        except BaseException as err:
            print(f"\n[!]Utils volume error:\n{err}")
            r = re.sub(r'[^.0-9]', r'', str(value))
            value = round(float(r)) if r else None

        if not value:
            em.title = "Громкость"
            em.description = f"`{int(raw)}%`"
            await ctx.send(embed=em)
            return None, None

        if value < 1:
            value = 1
            await ctx.send(embed=discord.Embed(description=f"Громкость не может быть ниже {value}%"))
        elif value > 250:
            value = 250
            await ctx.send(embed=discord.Embed(description=f"Громкость не может быть больше {value}"))
        if value == raw:
            await ctx.send(embed=discord.Embed(description="Новая громкость не может ровнятся старой"))
            return None, None

        return em, value

    async def reaction_roles(self, ctx: cmd.Context, message: str, args: tuple) -> Tuple[list, list, discord.Message]:
        msg: discord.Message = None
        if len(re.sub(r"[^0-9]", r"", f"{message}")) in range(15, 20):
            for i in ctx.guild.text_channels:
                try:
                    msg = await i.fetch_message(int(re.sub(r"[^0-9]", r"", f"{message}")))
                    if msg:
                        break
                except:
                    continue

        key = args[::2]
        k = []
        for i in key:
            print(i)
            ids = re.sub(r"[^0-9]", r"", f"{i}")
            if len(ids) in range(15, 20):
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
                if len(re.sub(r"[^0-9]", r"", f"{value[i]}")) in range(15, 20):
                    r_id = int(re.sub(r"[^0-9]", r"", f"{value[i]}"))
                    r = ctx.guild.get_role(r_id)
                    if not r:
                        raise cmd.BadArgument(f"Роль(*{r_id}*) не найдена")
                    v.append(r)
                elif len(ctx.message.role_mentions) > 0:
                    r = ctx.guild.get_role(ctx.message.role_mentions[m].id)
                    if not r:
                        raise cmd.BadArgument(f"Роль(*{ctx.message.role_mentions[m].id}*) не найдена")
                    m += 1
                    v.append(r)
            else:
                value = v

        return list(key), list(value), msg or message

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

    @staticmethod
    async def get_info(video_url: str) -> str:
        try:
            video_url = urllib.parse.quote(video_url)
            async with aiohttp.ClientSession() as session:
                async with session.get(f"https://geno.glitch.me/yt/{video_url}") as res:
                    info = await res.json()
                    info = info['videos']
                    info = "https://www.youtube.com/watch?v=" + info[0]['items'][0]['id'] if info and len(
                        info) else None

                    if not info:
                        return video_url

            return info
        except BaseException as err:
            print(f"\n[!]Utils get_info error:\n{err}")

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
            response: dict = request.execute()

            if response['pageInfo']['resultsPerPage'] < 1:

                if recurr:
                    raise cmd.BadArgument("Канал не найден")

                return self.search_channel(req, False, True)

            return response

        request = youtube.channels().list(part="snippet,contentDetails,statistics", forUsername=f"{req}", maxResults=1)
        response: dict = request.execute()

        if response['pageInfo']['resultsPerPage'] < 1:

            if recurr:
                raise cmd.BadArgument("Канал не найден")

            return self.search_channel(req, True, True)

        return response


class Paginator:
    def __init__(self, ctx: cmd.Context, begin: discord.Embed, embeds: Union[Tuple[discord.Embed], List[discord.Embed]],
                 timeout: int = 120, reactions: Union[Tuple[str], List[str]] = None):

        self.reactions: Union[Tuple[str], List[str]] = reactions or ('⬅', '⏹', '➡')
        self.pages = [i for i in embeds]
        self.current = 0
        self.ctx = ctx
        self.timeout = timeout
        self.begin = begin
        self.controller: discord.Message = None

    async def __close_session(self):
        try:
            await self.controller.delete()
        except:
            pass
        del self.pages
        del self.reactions
        del self.current
        del self.ctx

    async def start(self):
        self.controller = await self.ctx.send(embed=self.begin)

        try:
            await self.controller.add_reaction(self.reactions[2])
        except:
            return

        await self.ctx.bot.wait_for('reaction_add', timeout=self.timeout, check=lambda r, u: u.bot is not True)
        await self.__call_controller()

    async def __call_controller(self, start_page: int = 0):
        if not self.controller:
            self.controller = await self.ctx.send(embed=self.pages[start_page])
        else:
            await self.controller.edit(embed=self.pages[start_page])

        try:
            await self.controller.clear_reactions()
            for emoji in self.reactions:
                await self.controller.add_reaction(emoji)
        except:
            return

        while True:
            try:
                response = await self.ctx.bot.wait_for('reaction_add', timeout=self.timeout,
                                                       check=lambda r, u: u.id == self.ctx.author.id
                                                                          and r.emoji in self.reactions
                                                                          and r.message.id == self.controller.id)
            except TimeoutError:
                break

            try:
                await self.controller.remove_reaction(response[0], response[1])

                if response[0].emoji == self.reactions[0]:
                    self.current = self.current - 1 if self.current > 0 else len(self.pages) - 1
                    await self.controller.edit(embed=self.pages[self.current])

                elif response[0].emoji == self.reactions[1]:
                    break

                elif response[0].emoji == self.reactions[2]:
                    self.current = self.current + 1 if self.current < len(self.pages) - 1 else 0
                    await self.controller.edit(embed=self.pages[self.current])
            except:
                pass

        await self.__close_session()


class Twitch:
    def __init__(self, bot):
        self.bot = bot
        self.main: dict = bot.raw_main
        self.client_id = self.main['twitch_id']  # https://dev.twitch.tv/console/apps
        self.token = self.main['twitch_token']  # https://twitchapps.com/tokengen/  https://dev.twitch.tv/
        self.client_secret = self.main['twitch_secret']
        self.url = "https://api.twitch.tv/helix/search/"
        self.refresh_url = "https://id.twitch.tv/oauth2/token?grant_type=refresh_token&refresh_token={toke}&client_id={client_id}&client_secret={client_secret}"

    async def get_response(self, query, session) -> dict:
        async with session.get(f"{self.url}{query}",
                               headers={
                                   "Client-Id": self.client_id, 'Authorization': f'Bearer {self.token}',
                                   "Accept": "application/vnd.v5+json"
                               }) as response:
            res: dict = await response.json()

        if "data" not in res:

            if res['status'] == 404:
                res = {"data": []}

            elif res['status'] == 401:
                print("\n[!] Twitch no data")
                self.print_response(res)

                async with session.post(self.refresh_url.format(client_id=self.client_id, token=self.token,
                                                                client_secret=self.client_secret)) as response:
                    res = await response.json()
                    if "refresh_token" in res:
                        self.main['twitch_token'] = res['refresh_token']
                        await self.bot.main_cfg.update_one({"_id": 0}, {"$set": self.main})

        return res

    @staticmethod
    async def stream_embed(login, res1, res2, channel) -> discord.Message:
        em: discord.Embed = discord.Embed(title=f"{res1['title']}", url=f"https://twitch.tv/{login}",
                                          description=f"viewer count: `{res1['viewer_count']}`",
                                          colour=discord.Colour(value=int('6441a5', 16)),
                                          timestamp=datetime.now())

        em.set_author(name=f"{res1['user_name']}", url=f"https://twitch.tv/{login}",
                      icon_url=f"{res2['profile_image_url']}")

        em.set_image(url=f"{res1['thumbnail_url'].format(width=1920, height=1080)}")

        return await channel.send(embed=em)

    @staticmethod
    def get_stream_query(login) -> str:
        return f"streams?user_login={login}"

    @staticmethod
    def get_user_query(login) -> str:
        return f"users?login={login}"

    @staticmethod
    def print_response(res):
        print_res = json.dumps(res, indent=2)
        return print(print_res, "\n")


class Checks:
    def __init__(self, bot):
        self.bot = bot
        self.cmds: Collection = bot.cmds

    async def is_off(self, ctx: cmd.Context) -> bool:
        if ctx.author.id == self.bot.owner_id or ctx.author.id in self.bot.owner_ids or not ctx.guild:
            return True

        cfg: dict = await self.cmds.find_one({"_id": f"{ctx.guild.id}"})
        if not cfg:
            return True

        if ctx.command.name in cfg['commands'] or ctx.command.cog_name in cfg['cogs']:
            raise cmd.BadArgument("DISABLED")

        return True


class DataBase:
    def __init__(self, bot):
        self.bot = bot
        self.models = bot.models
        self.servers: Collection = bot.servers
        self.profiles: Collection = bot.profiles

    async def create(self):
        await asyncio.gather(self._create_servers())

    async def _create_servers(self):
        arr = [int(i['_id']) async for i in self.servers.find()]

        create = [self.models.Server(i).get_dict() for i in self.bot.guilds if i.id not in arr]
        if len(create):
            print("...servers creation")
            await self.servers.insert_many(create)

        del create, arr
        print("created: servers")

    async def _delete_servers(self):
        arr = [i['_id'] async for i in self.servers.find()]
        guilds = [str(i.id) for i in self.bot.guilds]
        delete = [{"_id": i} for i in arr if i not in guilds]

        await self.servers.delete_many(delete)

    async def create_server(self, guild: discord.Guild):
        i = self.models.Server(guild).get_dict()
        srv = await self.servers.find_one({"_id": f"{i['_id']}"})

        if not srv:
            await self.servers.insert_one(i)
            print(f"created: {i['_id']}")


class EmbedGenerator:
    @classmethod
    async def init(cls, target: str, inp: dict = None, **kwargs):
        if not inp:
            inp = {}
        for name, value in kwargs.items():
            inp[str(name)] = value

        if target == "queue":
            video = inp['video']
            ctx = inp['ctx']
            em = video.get_embed()

            embed = discord.Embed(title=video.title,
                                  url=video.video_url,
                                  colour=discord.Colour.green())

            embed.set_thumbnail(url=em.image.url)
            embed.set_author(name=f"{str(ctx.author)} add to queue:",
                             icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))
            return em

        elif target == "bot":
            system = inp['system']
            cpu = inp['cpu']
            ram = inp['ram']
            platform = inp['platform']
            ctx = inp['ctx']
            data = inp['data'].bot

            em = discord.Embed(title=f"{ctx.me.name} {data.version}",
                               colour=discord.Colour.green(),
                               timestamp=datetime.now())
            em.add_field(name="ОС:", value=f"`{system[0]} {system[2]}`")
            em.add_field(name="ЦП:", value=f'`{cpu}`')
            em.add_field(name="ОЗУ:", value=ram)
            em.add_field(name="Пользователей:", value=f"`{len([i.id for i in data.users if not i.bot])}`")
            em.add_field(name="Серверов:", value=f"`{len(data.guilds)}`")
            em.add_field(name='\u200b', value="\u200b")

            up = await data.utils.uptime()
            em.add_field(name="Ап-тайм:", value=f"`{up}`")

            em.add_field(name="Пинг вс:", value=f"`{round(data.latency * 1000, 1)}s`")
            em.add_field(name='\u200b', value="\u200b")
            em.add_field(name="Python версия:", value=f"`{platform.python_version()}`")
            em.add_field(name="Discord.Py версия:",
                         value=f"`{'.'.join(list(map(lambda x: str(x), list(discord.version_info)[:3])))}`")

            em.add_field(name='\u200b', value="\u200b")
            em.set_footer(text=str(ctx.author),
                          icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

            return em
