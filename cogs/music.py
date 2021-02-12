# -*- coding: utf-8 -*-
import asyncio
import re
from datetime import datetime
from typing import Union

import lavalink

import discord
from bot.client import Geno, geno
from discord.ext import commands as cmd

url_rx = re.compile(r'https?://(?:www\.)?.+')
checks = geno.checks


class Music(cmd.Cog):
    def __init__(self, bot: Geno):
        self.bot = bot
        self.config = bot.servers

        bot.lavalink = lavalink.Client(bot.user.id)
        bot.lavalink.add_node('localhost', 8080, 'lavalavago', 'russia', 'music-node')
        bot.add_listener(bot.lavalink.voice_update_handler, 'on_socket_response')

        lavalink.add_event_hook(self.track_hook)

    def cog_unload(self):
        self.bot.lavalink._event_hooks.clear()

    async def cog_before_invoke(self, ctx):
        guild_check = ctx.guild is not None

        if guild_check:
            await self.ensure_voice(ctx)

        return guild_check

    async def ensure_voice(self, ctx):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)
        player: lavalink.DefaultPlayer = self.bot.lavalink.player_manager.create(ctx.guild.id,
                                                                                 endpoint=str(
                                                                                     ctx.guild.region)) if not player else player
        should_connect = ctx.command.name in ('Play', 'Join',)

        if not ctx.author.voice or not ctx.author.voice.channel:
            raise cmd.CommandInvokeError('Вы должны быть в голосовом канале, для использования этой команды')

        if not player.is_connected:
            if not should_connect:
                raise cmd.CommandInvokeError('Нет подключения, попробуйте использовать `join`')

            permissions = ctx.author.voice.channel.permissions_for(ctx.me)

            if not permissions.connect or not permissions.speak:
                perms = "`CONNECT` и `SPEAK`" if not permissions.connect and not permissions.speak else \
                    "`CONNECT`" if not permissions.connect and permissions.speak else "`SPEAK`"
                raise cmd.BotMissingPermissions(perms)

            player.store('channel', ctx.channel.id)
            await self.connect_to(ctx.guild.id, str(ctx.author.voice.channel.id))
        else:
            if int(player.channel_id) != ctx.author.voice.channel.id and ctx.command.name != "Join":
                raise cmd.CommandInvokeError('Вы должны быть в одном канале сомной')

    async def track_hook(self, event):
        if isinstance(event, lavalink.events.QueueEndEvent):
            try:
                player = event.player
                if not player or not player.guild_id:
                    return

                cfg = await self.config.find_one({"_id": f"{player.guild_id}"})
                if not cfg:
                    return
                cfg = cfg['music']

                guild_id = int(player.guild_id)

                cfg['queue'] = []
                cfg['now_playing'] = ""

                await self.config.update_one({"_id": f"{guild_id}"}, {"$set": {"music": dict(cfg)}})

                await asyncio.sleep(60)

                cfg = await self.config.find_one({"_id": guild_id})
                cfg = cfg['music']
                try:
                    guild = self.bot.get_guild(int(guild_id))
                    if not guild:
                        return
                    ch = guild.get_channel(int(player.channel_id or 1))
                    if not ch:
                        return

                    if cfg['now_playing'] and ch and player.is_connected and len(ch.members) <= 1:
                        player.queue.clear()
                        await player.stop()
                        await self.connect_to(guild_id)
                        await guild.get_channel(cfg['last']['channel']).send("Авто-выход, конец очереди проигрывания")

                except BaseException as err:
                    print(f"queue end error guild\n{err}")
            except BaseException as err:
                print(f"\n[!]Music track_hook queue error:\n{err}\n")

        elif isinstance(event, lavalink.TrackStartEvent):
            try:
                player = event.player
                if not player:
                    return

                cfg = await self.config.find_one({"_id": f"{event.player.guild_id}"})
                cfg = cfg['music']
                data = self.bot.utils.now_playing(player=player)

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

                await self.config.update_one({"_id": f"{player.guild_id}"}, {"$set": {"music": dict(cfg)}})

                em = discord.Embed(title=cfg['now_playing']['title'],
                                   description=f"Длительность: `{lavalink.format_time(int(player.current.duration))}`"
                                               f"\nТэги: `{cfg['now_playing']['tags'][0]}`",
                                   url=cfg['now_playing']['video_url'],
                                   timestamp=datetime.now(),
                                   colour=discord.Colour.green())
                em.set_image(url=cfg['now_playing']['thumbnail'])
                em.set_author(name=cfg['now_playing']['name'], url=cfg['now_playing']['url'],
                              icon_url=cfg['now_playing']['icon'])
                em.set_footer(text=f"Добавил в очередь: {str(data['req'])}",
                              icon_url=data['req'].avatar_url_as(format='png', static_format='png', size=256))

                try:
                    message = await self.bot.get_guild(int(player.guild_id)).get_channel(
                        int(cfg['last']['channel'])).send(
                        embed=em)
                    cfg['last'] = {"message": f"{message.id}", "channel": f"{message.channel.id}"}
                    await self.config.update_one({"_id": f"{player.guild_id}"}, {"$set": {"music": dict(cfg)}})

                except BaseException as err:
                    print(f"track start error guild\n{err}")

            except BaseException as err:
                print(f"\n[!]Music track_hook start error:\n{err}\n")

        elif isinstance(event, lavalink.TrackEndEvent):
            try:
                player = event.player
                if not player:
                    return

                cfg = await self.config.find_one({"_id": f"{player.guild_id}"})
                cfg = cfg['music']

                try:
                    guild = self.bot.get_guild(int(player.guild_id))

                    message = await guild.get_channel(int(cfg['last']['channel'])).fetch_message(
                        int(cfg['last']['message']))
                    if message:
                        await message.delete()

                    ch = guild.get_channel(int(player.channel_id or 1))
                    if ch and len(ch.members) <= 1:
                        player.queue.clear()
                        await player.stop()
                        await self.connect_to(guild.id)

                        cfg['queue'] = []
                        cfg['now_playing'] = ""
                        ch = cfg['last']['channel']
                        await self.config.update_one({"_id": str(player.guild_id)}, {"$set": {"music": dict(cfg)}})

                        await guild.get_channel(ch).send("Авто-выход, пустой канал")

                except BaseException as err:
                    print(f"track end error guild\n{err}")

            except BaseException as err:
                print(f"\n[!]Music track_hook end error:\n{err}\n")

    # async def track_hook(self, event):
    #     if event and not isinstance(event, lavalink.NodeConnectedEvent) and event.player:
    #         cfg = await self.config.find_one({"_id": event.player.guild_id})
    #         cfg = cfg['music']
    #
    #         channel: discord.VoiceChannel = self.bot.get_channel(int(event.player.channel_id or 1))
    #         txt: discord.TextChannel = self.bot.get_channel(int(cfg['last']['channel']) or 1)
    #
    #     if isinstance(event, lavalink.QueueEndEvent):
    #         try:
    #             await asyncio.sleep(60)
    #             cfg = await self.config.find_one({"_id": event.player.guild_id})
    #             if not cfg['music']['now_playing'] and not cfg['music']['queue']:
    #                 await self.connect_to(event.player.guild_id)
    #
    #         except BaseException as err:
    #             print(f"\n[!]Music track_hook queue error:\n{err}")
    #     elif isinstance(event, lavalink.TrackStartEvent):
    #         try:
    #             pass
    #         except BaseException as err:
    #             print(f"\n[!]Music track_hook start error:\n{err}")
    #
    #     elif isinstance(event, lavalink.TrackEndEvent):
    #         try:
    #             if channel and len(channel.members) <= 1:
    #                 msg: discord.Message = await txt.fetch_message(cfg['last']['message'])
    #                 await msg.delete()
    #                 await self.connect_to(txt.guild.id)
    #
    #         except BaseException as err:
    #             print(f"\n[!]Music track_hook end error:\n{err}")

    async def connect_to(self, guild_id: int, channel_id: str = None):
        await self.bot._connection._get_websocket(guild_id).voice_state(str(guild_id), channel_id)

    @cmd.command(name="Play", aliases=['p'], usage="play <url | query>", description="""
    Поддерживаются платформы: `Youtube, SoundCLoud`
    
    url - должен начинатся с `https://`,
     примеры: `https://www.youtube.com/watch?v=bM7SZ5SBzyY`, `https://soundcloud.com/nocopyrightsounds/alan-walker-fade-ncs-release`
    
    query - должен быть **полным** именем автора и/или названия трека,
     примеры: `Ncs`, `NoCopyrightSounds`, `Alan Walker - Fade [NCS Release]`
    
    проигрывает или добавляет в очередь - трек, который был задан ссылкой(`url`) или запросом(`query`)
    """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    @cmd.cooldown(1, 5, cmd.BucketType.guild)
    async def play(self, ctx: cmd.Context, *, query: str = None):
        if not query:
            raise cmd.BadArgument("Введите *ссылку* или *запрос* для поиска")

        tracks: list = []
        track: dict = {}
        results: dict = {}
        player: lavalink.DefaultPlayer = self.bot.lavalink.player_manager.get(ctx.guild.id)
        cfg: dict = await self.config.find_one({"_id": f"{ctx.guild.id}"})

        if url_rx.match(query):
            results = await player.node.get_tracks(query)
        else:
            for i in ['yt', 'sc']:
                results = await player.node.get_tracks(f'{i}search:{query}')
                if results or ('loadType' in results and results['loadType'] != 'NO_MATCHES'):
                    break

            if not results or 'tracks' not in results or not results['tracks']:
                results = await player.node.get_tracks(await self.bot.utils.get_info(video_url=query))

        if not results or 'tracks' not in results or not results['tracks']:
            raise cmd.BadArgument('Ничего не найдено')

        if results['loadType'] == 'PLAYLIST_LOADED':
            tracks = results['tracks']

            for track in tracks:
                player.add(requester=ctx.author.id, track=track)

            desc = f"[{results['playlistInfo']['name']}]({query})" if url_rx.match(query) else results["playlistInfo"][
                "name"]
            await ctx.send(embed=discord.Embed(title="Загружен плейлист:",
                                               description=f'{desc} - {len(tracks)} треков'))
        else:
            track = results['tracks'][0]
            player.add(requester=ctx.author.id, track=track)

        if not player.is_playing:
            cfg['music']['last'] = {"channel": f"{ctx.channel.id}"}
            await self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(cfg['music'])}})
            await player.play()
            await player.set_volume(cfg['music']['volume'] * 100)
        else:
            data = self.bot.utils.uploader(track, typ="yt" if "youtube.com" in track['info']['uri'] else "sc")
            em = discord.Embed(title=data['title'],
                               description=f"Длительность: `{data['duration']}`\nТэги: `{data['tags']}`",
                               colour=discord.Colour.green(),
                               url=data['video_url'],
                               timestamp=datetime.now())
            em.set_thumbnail(url=data['thumbnail'])
            em.set_author(name=data['name'], url=data['url'], icon_url=data['icon'])
            em.set_footer(text=str(ctx.author),
                          icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

            await ctx.send(embed=em)

    @cmd.command(name="Leave",
                 aliases=['dc', 'l', 'disconnect', 'stop', 'стоп', 'отключится', 'откл', 'д'], usage="stop",
                 description="""
        Выходит из голосового канала, если находится в нем, останавливает музыку с очисткой очереди
        """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    @cmd.cooldown(1, 5, cmd.BucketType.guild)
    async def stop(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('Вы должны быть в одном канале сомной')

        player.queue.clear()
        await player.stop()
        await self.connect_to(ctx.guild.id)

    @cmd.command(name="Volume", aliases=['v', 'громкость', 'г'], usage="volume `[value]`",
                 description="""
        value - может быть целым числом в виде процентов,
         примеры: `1`, `100`, `123`, `250`,
         по умолчанию: отображает текущую громкость

        Изменяет громкость музыки на `value`%
        """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    @cmd.cooldown(1, 5, cmd.BucketType.guild)
    async def volume(self, ctx: cmd.Context, value: Union[int, float] = None):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('Вы должны быть в одном канале сомной')

        cfg = await self.config.find_one({"_id": f"{ctx.guild.id}"})
        cfg = cfg['music']

        raw = int(player.volume)

        em, value = await self.bot.utils.volume(value, raw, ctx)
        if not em or not value:
            return

        await player.set_volume(value)
        cfg['volume'] = round(value / 100, 2)
        await self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(cfg)}})
        end = player.volume
        em.description = em.description.format(raw=int(raw), end=int(end))

        await ctx.send(embed=em)

    @cmd.command(name="Queue", aliases=['q', 'очередь', 'о'], usage="queue", description="""
        Показывает список заказамой музыки
        """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    @cmd.cooldown(1, 5, cmd.BucketType.guild)
    async def queue(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('Вы должны быть в одном канале сомной')

        embeds = await self.bot.utils.queue(ctx, player)

        if len(embeds) == 1:
            await ctx.send(embed=embeds[0])
        else:
            p = self.bot.Paginator(ctx, embeds=embeds)
            await p.call_controller()

    @cmd.command(name="Skip", aliases=['s', 'с', 'скип'], usage="skip", description="""
        Пропускает текущий трек
        """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    @cmd.cooldown(1, 5, cmd.BucketType.guild)
    async def skip(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('Вы должны быть в одном канале сомной')

        await player.skip()

    @cmd.command(name="Now Playing", aliases=['np', 'сейчас_играет', 'си'], usage="now_playing",
                 description="""
        Показывает информацию о текущем треке
        """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    @cmd.cooldown(1, 5, cmd.BucketType.guild)
    async def now_playing(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('Вы должны быть в одном канале сомной')

        data = await self.config.find_one({"_id": f"{ctx.guild.id}"})
        data = data['music']['now_playing']
        if data:
            data['req'] = ctx.guild.get_member(int(data['req']))

        em = discord.Embed(description=f"Длительность:"
                                       f" `{self.bot.utils.parser(start=data['start'], end=datetime.now(), typ='time') if data else '00:00:00'}` /"
                                       f" `{lavalink.format_time(int(player.current.duration)) if data else '00:00:00'}`"
                                       f"\nДобавил в очередь: `{str(data['req']) if data else 'None'}` [{data['req'].mention if data else 'None'}]",
                           title=player.current.title if data else discord.Embed.Empty,
                           url=player.current.uri if data else discord.Embed.Empty,
                           timestamp=datetime.now(),
                           colour=discord.Colour.green() if data else discord.Colour.dark_gold())
        em.set_thumbnail(url=data[
            'thumbnail'] if data else "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg")
        em.set_author(name=data['name'] if data else "Ничего не проигрывается",
                      url=data['url'] if data else discord.Embed.Empty,
                      icon_url=data[
                          'icon'] if data else "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg")
        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

        await ctx.send(embed=em)

    @cmd.command(name="Pause", aliases=['пауза'], usage="pause", description="""
        Ставит музыку на паузу, проигрывание музыки
        """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    @cmd.cooldown(1, 2, cmd.BucketType.guild)
    async def pause(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('Вы должны быть в одном канале сомной')

        if player.is_playing and not player.paused:
            await player.set_pause(True)

    @cmd.command(name="Resume", aliases=['продолжить', 'прдлж'], usage="resume", description="""
        Снимает с паузы, проигрывание музыки
        """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    @cmd.cooldown(1, 5, cmd.BucketType.guild)
    async def resume(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('Вы должны быть в одном канале сомной')

        if player.is_playing and player.paused:
            await player.set_pause(False)

    @cmd.command(name="Join", aliases=['j', 'присоединится', 'джоин', 'дж'], usage="join",
                 description="""
        Подключается к вашему голосовому каналу
        """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    @cmd.cooldown(1, 5, cmd.BucketType.guild)
    async def join(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)
        users = [i.id for i in ctx.guild.get_channel(int(player.channel_id or ctx.author.voice.channel.id)).members if
                 not i.bot]

        if not player.is_connected:
            return await self.connect_to(int(ctx.guild.id), str(ctx.author.voice.channel.id))

        if player.is_connected and ctx.author.voice.channel.id != int(player.channel_id):

            if len(users) < 1 or not player.current or (player.current and int(player.current.requester) in users):
                return await self.connect_to(int(ctx.guild.id), str(ctx.author.voice.channel.id))

            raise cmd.BadArgument('Я уже подключен к каналу')


def setup(bot: Geno):
    bot.add_cog(Music(bot))
