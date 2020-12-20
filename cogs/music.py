# -*- coding: utf-8 -*-

import asyncio
import re
from datetime import datetime

import lavalink

import discord
import discord.gateway
from bot.bot import Geno
from bot.bot import bot as b
from discord.ext import commands as cmd

url_rx = re.compile(r'https?://(?:www\.)?.+')
checks = b.checks


class Music(cmd.Cog):
    def __init__(self, bot: Geno):
        self.bot = bot
        self.config = bot.servers
        self.Paginator = bot.Paginator
        self.utils = bot.utils
        self.models = bot.models

        if not hasattr(bot, 'lavalink'):
            bot.lavalink = lavalink.Client(bot.user.id)
            bot.lavalink.add_node('localhost', 8080, 'lavalavago', 'eu', 'music-node')
            bot.add_listener(bot.lavalink.voice_update_handler, 'on_socket_response')

        lavalink.add_event_hook(self.track_hook)

    def cog_unload(self):
        """ Cog unload handler. This removes any event hooks that were registered. """
        self.bot.lavalink._event_hooks.clear()

    async def cog_before_invoke(self, ctx):
        """ Command before-invoke handler. """
        guild_check = ctx.guild is not None

        if guild_check:
            await self.ensure_voice(ctx)

        return guild_check

    async def ensure_voice(self, ctx):
        """ This check ensures that the bot and command author are in the same voicechannel. """
        player = self.bot.lavalink.player_manager.create(ctx.guild.id, endpoint=str(ctx.guild.region))
        should_connect = ctx.command.name in ('Play', 'Join',)

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
            if int(player.channel_id) != ctx.author.voice.channel.id and ctx.command.name != "Join":
                raise cmd.CommandInvokeError('You need to be in my voicechannel.')

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

                print("\nQueueEndEvent")
                print(player.guild_id)

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
                        await guild.get_channel(cfg['last']['channel']).send("End of playback, auto disconnect")
                except BaseException as err:
                    print(f"queue end error guild\n{err}")
            except BaseException as err:
                print("\n", "-" * 30, f"\n[!]Music track_hook queue error:\n{err}\n", "-" * 30, "\n")

        elif isinstance(event, lavalink.TrackStartEvent):
            try:
                player = event.player
                if not player:
                    return

                cfg = await self.config.find_one({"_id": f"{event.player.guild_id}"})
                cfg = cfg['music']
                data = self.utils.now_playing(player=player)

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

                print("\nTrackStartEvent")
                print(player.guild_id)

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

                try:
                    message = await self.bot.get_guild(int(player.guild_id)).get_channel(
                        int(cfg['last']['channel'])).send(
                        embed=em)
                    cfg['last'] = {"message": f"{message.id}", "channel": f"{message.channel.id}"}
                    await self.config.update_one({"_id": f"{player.guild_id}"}, {"$set": {"music": dict(cfg)}})
                except BaseException as err:
                    print(f"track start error guild\n{err}")

            except BaseException as err:
                print("\n", "-" * 30, f"\n[!]Music track_hook start error:\n{err}\n", "-" * 30, "\n")

        elif isinstance(event, lavalink.TrackEndEvent):
            try:
                player = event.player
                if not player:
                    return

                cfg = await self.config.find_one({"_id": f"{player.guild_id}"})
                cfg = cfg['music']

                print("\nTrackEndEvent")
                print(player.guild_id)

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

                        await guild.get_channel(ch).send("Empty voice channel, auto disconnect")
                except BaseException as err:
                    print(f"track end error guild\n{err}")

            except BaseException as err:
                print("\n", "-" * 30, f"\n[!]Music track_hook end error:\n{err}\n", "-" * 30, "\n")

    async def connect_to(self, guild_id: int, channel_id: str = None):
        """ Connects to the given voicechannel ID. A channel_id of `None` means disconnect. """
        ws = self.bot._connection._get_websocket(guild_id)
        await ws.voice_state(str(guild_id), channel_id)

    @cmd.command(name="Play", aliases=['p', 'п', 'плей', 'играть', 'и'], usage="play `<url | query>`",
                 description="""
    Supported platforms: `Youtube, SoundCLoud`
    
    url - must be starts with **https://**,
     examples: `https://www.youtube.com/watch?v=286jXjwdst0`, `https://soundcloud.com/kira_productions/vocaloid-original-circles-gumi-english`
    
    query - must be a **full** name of author or/and track title,
     examples: `Apashe`, `Apashe uebok`, `Apashe ft. Instasamka - Uebok (Gotta Run) [Official Video]`
    
    plays or add in queue - track, that was given as `url` or `query`
    :-:
    Поддерживаются платформы: `Youtube, SoundCLoud`
    
    url - должен начинатся с `https://`,
     примеры: `https://www.youtube.com/watch?v=286jXjwdst0`, `https://soundcloud.com/kira_productions/vocaloid-original-circles-gumi-english`
    
    query - должен быть **полным** именем автора и/или названия трека,
     примеры: `Apashe`, `Apashe uebok`, `Apashe ft. Instasamka - Uebok (Gotta Run) [Official Video]`
    
    проигрывает или добавляет в очередь - трек, который был задан ссылкой(`url`) или запросом(`query`)
    """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    async def play(self, ctx: cmd.Context, *, query: str):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)
        track = None
        query = query.strip('<>')
        services = ['yt', 'sc']
        results = None
        cfg = await self.config.find_one({"_id": f"{ctx.guild.id}"})
        cfg = cfg['music']

        if not url_rx.match(query):
            for i in services:
                query = f'{i}search:{query}'
                results = await player.node.get_tracks(query)
                if results or ('loadType' in results and results['loadType'] != 'NO_MATCHES'):
                    results = None
                    break

            if not results or not results['tracks']:
                url = await self.utils.get_info(video_url=query.split(":")[1])
                results = await player.node.get_tracks(url)
        else:
            results = await player.node.get_tracks(query)

        if not results or not results['tracks']:
            raise cmd.BadArgument('Nothing found!')

        if results['loadType'] == 'PLAYLIST_LOADED':
            tracks = results['tracks']

            for track in tracks:
                player.add(requester=ctx.author.id, track=track)

            desc = f'[{results["playlistInfo"]["name"]}]({query})' if url_rx.match(query) \
                else results["playlistInfo"]["name"]

            await ctx.send(embed=discord.Embed(title="Loaded playlist:",
                                               description=f'{desc} - {len(tracks)} tracks'))
        else:
            track = results['tracks'][0]
            track = lavalink.models.AudioTrack(data=track, requester=ctx.author.id)
            player.add(requester=ctx.author.id, track=track)

            track = results['tracks'][0]

        if not player.is_playing:
            cfg['last'] = {"channel": f"{ctx.channel.id}"}
            await self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(cfg)}})
            await player.play()
            await player.set_volume(cfg['volume'] * 100)
        else:
            data = self.utils.uploader(track, typ="yt" if "youtube.com" in track['info']['uri'] else "sc")
            em = discord.Embed(description=f"Duration: `{data['duration']}`\nTags: `{data['tags']}`",
                               timestamp=datetime.now(),
                               colour=discord.Colour.green(),
                               title=data['title'],
                               url=data['video_url'])
            em.set_thumbnail(url=data['thumbnail'])
            em.set_author(name=data['name'], url=data['url'], icon_url=data['icon'])
            em.set_footer(text=str(ctx.author),
                          icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))
            await ctx.send(embed=em)

    @cmd.command(name="Leave",
                 aliases=['dc', 'l', 'disconnect', 'stop', 'стоп', 'отключится', 'откл', 'д'], usage="stop",
                 description="""
    Leaves from voice channel if is in it, and stops the music with cleanup queue
    :-:
    Выходит из голосового канала, если находится в нем, и останавливает музыку с очисткой очереди
    """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    async def stop(self, ctx: cmd.Context):
        """ Disconnects the player from the voice channel and clears its queue. """
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        player.queue.clear()
        await player.stop()
        await self.connect_to(ctx.guild.id)

    @cmd.command(name="Volume", aliases=['v', 'громкость', 'г'], usage="volume `[value]`",
                 description="""
    value - can be an integer number as percents,
     example: `1`, `100`, `123`, `250`,
     default: show current volume
     
    Change music volume into `value`%
    :-:
    value - может быть целым числом в виде процентов,
     примеры: `1`, `100`, `123`, `250`,
     по умолчанию: отображает текущую громкость
     
    Изменяет громкость музыки на `value`%
    """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    async def volume(self, ctx: cmd.Context, value=None):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        cfg = await self.config.find_one({"_id": f"{ctx.guild.id}"})
        cfg = cfg['music']

        raw = int(player.volume)

        em, value = await self.utils.volume(value, raw, ctx)
        if not em or not value:
            return

        await player.set_volume(value)
        cfg['volume'] = round(value / 100, 2)
        await self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(cfg)}})
        end = player.volume
        em.description = em.description.format(raw=int(raw), end=int(end))

        await ctx.send(embed=em)

    @cmd.command(name="Queue", aliases=['q', 'очередь', 'о'], usage="queue", description="""
    Returns ito chat back list of music list
    :-:
    Возвращает в чат список заказамой музыки
    """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    async def queue(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        embeds = await self.utils.queue(ctx, player)

        if len(embeds) == 1:
            await ctx.send(embed=embeds[0])
        else:
            p = self.Paginator(ctx, embeds=embeds)
            await p.call_controller()

    @cmd.command(name="Skip", aliases=['s', 'с', 'скип'], usage="skip", description="""
    Skip current track to next in queue
    :-:
    Пропускает текущий трек к следущему в списке
    """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    async def skip(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        await player.skip()

    @cmd.command(name="Now Playing", aliases=['np', 'сейчас_играет', 'си'], usage="now_playing",
                 description="""
    Returns into chat back current track info
    :-:
    Возвращает в чат информацию о текущем треке
    """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    async def now_playing(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        data = await self.config.find_one({"_id": f"{ctx.guild.id}"})
        data = data['music']['now_playing']
        if data:
            data['req'] = ctx.guild.get_member(int(data['req']))

        em = discord.Embed(description=f"Duration:"
                                       f" `{self.utils.parser(start=data['start'], end=datetime.now(), typ='time') if data else '00:00:00'}` /"
                                       f" `{lavalink.format_time(int(player.current.duration)) if data else '00:00:00'}`"
                                       f"\nRequested by: `{str(data['req']) if data else 'None'}` [{data['req'].mention if data else 'None'}]",
                           title=player.current.title if data else discord.Embed.Empty,
                           url=player.current.uri if data else discord.Embed.Empty,
                           timestamp=datetime.now(),
                           colour=discord.Colour.green() if data else discord.Colour.dark_gold())
        em.set_thumbnail(url=data[
            'thumbnail'] if data else "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg")
        em.set_author(name=data['name'] if data else "Seems like nothing is plying now",
                      url=data['url'] if data else discord.Embed.Empty,
                      icon_url=data[
                          'icon'] if data else "https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg")
        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

        await ctx.send(embed=em)

    @cmd.command(name="Pause", aliases=['пауза'], usage="pause", description="""
    Pausing music playback
    :-:
    тавит музыку на паузу
    """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    async def pause(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        if player.is_playing and not player.paused:
            await player.set_pause(True)

    @cmd.command(name="Resume", aliases=['продолжить', 'прдлж'], usage="resume", description="""
    Resume - unpausing music playback
    :-:
    Продолжает - снимает с паузы, проигрывание музыки
    """)
    @cmd.check(checks.is_off)
    @cmd.guild_only()
    async def resume(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        if player.is_playing and player.paused:
            await player.set_pause(False)

    @cmd.command(name="Join", aliases=['j', 'присоединится', 'джоин', 'дж'], usage="join",
                 description="""
    Joins to your voice-channel
    :-:
    одключается к вашему голосовому каналу
    """)
    @cmd.guild_only()
    async def join(self, ctx: cmd.Context):
        if not ctx.author.voice:
            raise cmd.BadArgument("You must be in voicechannel to use this command.")

        player = self.bot.lavalink.player_manager.get(ctx.guild.id)
        users = [i.id for i in ctx.guild.get_channel(int(player.channel_id or ctx.author.voice.channel.id)).members if
                 not i.bot]

        if not player.is_connected:
            return await self.connect_to(int(ctx.guild.id), str(ctx.author.voice.channel.id))

        if player.is_connected and ctx.author.voice.channel.id != int(player.channel_id):

            if len(users) < 1 or not player.current or (player.current and int(player.current.requester) in users):
                return await self.connect_to(int(ctx.guild.id), str(ctx.author.voice.channel.id))

            raise cmd.BadArgument('I\'m already connected to some voice-channel!')


def setup(bot):
    bot.add_cog(Music(bot))
