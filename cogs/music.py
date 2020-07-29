# -*- coding: utf-8 -*-
import re
import threading
from datetime import datetime
from os import system

import lavalink

import discord
import discord.gateway
from discord.ext import commands as cmd

url_rx = re.compile(r'https?://(?:www\.)?.+')


class Music(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.config = bot.servers
        self.Paginator = bot.Paginator
        self.utils = bot.utils
        self.models = bot.models
        
        for i in range(1):
            threading.Thread(target=system, args=("java -jar s/Lavalink.jar",)).start()

        if not hasattr(bot, 'lavalink'):
            bot.lavalink = lavalink.Client(bot.user.id)
            bot.lavalink.add_node('localhost', 8080, 'lavalavago', 'eu', 'music-node')
            bot.add_listener(bot.lavalink.voice_update_handler, 'on_socket_response')

        lavalink.add_event_hook(self.utils.track_hook)

    def cog_unload(self):
        """ Cog unload handler. This removes any event hooks that were registered. """
        self.bot.lavalink._event_hooks.clear()

    async def cog_before_invoke(self, ctx):
        """ Command before-invoke handler. """
        guild_check = ctx.guild is not None

        if guild_check:
            await self.utils.ensure_voice(ctx)

        return guild_check

    @cmd.command(name="Play", aliases=['p', 'play', 'п', 'плей', 'играть'], usage="play `<url | query>`",
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
    @cmd.guild_only()
    async def _play(self, ctx: cmd.Context, *, query: str):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        query = query.strip('<>')
        services = ['yt', 'sc']
        results = None
        cfg = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']

        if not url_rx.match(query):
            for i in services:
                query = f'{i}search:{query}'
                results = await player.node.get_tracks(query)
                if results:
                    break

            if not results or not results['tracks']:
                results = await player.node.get_tracks(self.utils.get_info(query)["webpage_url"])
        else:
            results = await player.node.get_tracks(query)

        if not results or not results['tracks']:
            raise cmd.BadArgument('Nothing found!')

        if results['loadType'] == 'PLAYLIST_LOADED':
            tracks = results['tracks']
            print(results)

            for track in tracks:
                player.add(requester=ctx.author.id, track=track)

            desc = f'({results["playlistInfo"]["name"]})[{query}]' if url_rx.match(query) \
                else results["playlistInfo"]["name"]

            await ctx.send(embed=discord.Embed(title="Loaded playlist:",
                                               description=f'{desc} - {len(tracks)} tracks'))
        else:
            track = results['tracks'][0]
            track = lavalink.models.AudioTrack(track, ctx.author.id, recommended=True)
            player.add(requester=ctx.author.id, track=track)

            track = results['tracks'][0]
        
        if not player.is_playing:
            cfg['last'] = {"channel": f"{ctx.channel.id}"}
            self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(cfg)}})
            await player.play()
            await player.set_volume(cfg['volume']*100)
        else:
            data = self.utils.uploader(track, typ="yt" if "youtube.com" in track['info']['uri'] else "sc")
            em = discord.Embed(description=f"Duration: `{data['duration']}`"
                                           f"\nTags: `{data['tags']}`",
                               timestamp=datetime.now(),
                               colour=discord.Colour.green())
            em.set_thumbnail(url=data['thumbnail'])
            em.set_author(name=data['name'], url=data['url'], icon_url=data['icon'])
            em.set_footer(text=str(ctx.author),
                          icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))
            await ctx.send(embed=em)

    @cmd.command(name="Leave",
                 aliases=['dc', 'leave', 'l', 'disconnect', 'stop', 'стоп', 'отключится', 'откл', 'д'], usage="stop",
                 description="""
    Leaves from voice channel if is in it, and stops the music with cleanup queue
    
    You must be in same bot's voice channel
    :-:
    Выходит из голосового канала, если находится в нем, и останавливает музыку с очисткой очереди
    
    Вы должны быть в одном канале с ботом
    """)
    @cmd.guild_only()
    async def _stop(self, ctx: cmd.Context):
        """ Disconnects the player from the voice channel and clears its queue. """
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        player.queue.clear()
        await player.stop()
        await self.utils.connect_to(ctx.guild.id, None)

    @cmd.command(name="Volume", aliases=['volume', 'v', 'громкость', 'г'], usage="volume `[value]`",
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
    @cmd.guild_only()
    async def _volume(self, ctx: cmd.Context, value=None):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        cfg = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']
        em = discord.Embed(title="Volume change", description="From: `{raw}%`\nTo: `{end}%`",
                           colour=discord.Colour.green(), timestamp=datetime.now())
        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))
        raw = player.volume

        if not value:
            em.title = "Volume"
            em.description = f"`{player.volume}%`"
            await ctx.send(embed=em)
            return

        try:
            value = int(value)
        except:
            r = re.sub(r'[^.0-9]', r'', value)
            value = round(float(r)) if r else None

        if not value:
            em.title = "Volume"
            em.description = f"`{player.volume}%`"
            await ctx.send(embed=em)
            return

        if value < 1:
            value = 1
            await ctx.send(embed=discord.Embed(description=f"Volume value can't be less than {value}%"))
        elif value > 250:
            value = 250
            await ctx.send(embed=discord.Embed(description=f"Volume value can't be more than {value}"))
        if value == raw:
            return await ctx.send(embed=discord.Embed(description="New volume value can't equals to old"))

        await player.set_volume(value)
        cfg['volume'] = round(value / 100, 2)
        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(cfg)}})
        end = player.volume
        em.description = em.description.format(raw=raw, end=end)

        await ctx.send(embed=em)

    @cmd.command(name="Queue", aliases=['q', 'queue'], usage="queue", description="""
    Returns ito chat back list of music list
    :-:
    Возвращает в чат список заказамой музыки
    """)
    @cmd.guild_only()
    async def _queue(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        queue = [{"data": self.utils.search_video(req=i.identifier), "req": ctx.guild.get_member(int(i.requester))}
                 for i in player.queue]
        description = [f"`{i + 1}`. Requested by: {str(queue[i]['req'])}\n" \
                       f"[{queue[i]['data']['items'][0]['snippet']['title']}]" \
                       f"(https://www.youtube.com/watch?v={queue[i]['data']['items'][0]['id']})" for i in
                       range(len(queue))]

        data = self.utils.now_playing(player)
        dir(player.current)
        em = discord.Embed(description="\n".join(description),
                           title=player.current.title,
                           url=player.current.uri,
                           colour=discord.Colour.green(),
                           timestamp=datetime.now())
        em.set_thumbnail(url=data['img']['url'])
        em.set_author(name=data['csnipp']['title'],
                      url=f"https://youtube.com/channel/{data['channel']['items'][0]['id']}",
                      icon_url=data['icon']['url'])

        await ctx.send(embed=em)

    @cmd.command(name="Skip", aliases=['s', 'skip'], usage="skip", description="""
    Skip current track to next in queue
    :-:
    Пропускает текущий трек к следущему в списке
    """)
    @cmd.guild_only()
    async def _skip(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        await player.skip()

    @cmd.command(name="Now Playing", aliases=['np', 'now_playing'], usage="now_playing", description="""
    Returns into chat back current track info
    :-:
    Возвращает в чат информацию о текущем треке
    """)
    @cmd.guild_only()
    async def _now_playing(self, ctx: cmd.Context):
        player = self.bot.lavalink.player_manager.get(ctx.guild.id)

        if not player.is_connected:
            raise cmd.BadArgument('Not connected.')

        if not ctx.author.voice or (player.is_connected and ctx.author.voice.channel.id != int(player.channel_id)):
            raise cmd.BadArgument('You\'re not in my voicechannel!')

        data = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']['now_playing']
        data['req'] = ctx.guild.get_member(int(data['req']))

        em = discord.Embed(title=data['title'],
                           description=f"Duration:"
                                       f" `{self.utils.parser(start=data['start'], end=datetime.now(), typ='time')}` "
                                       f"/ `{lavalink.format_time(int(player.current.duration)), typ='time')}`"
                                       f"\nRequested by: `{str(data['req'])}` [{data['req'].mention}]",
                           url=player.current.uri)
        em.set_thumbnail(url=data['thumbnail'])
        em.set_author(name=data['name'], url=data['url'],
                      icon_url=data['icon'])
        em.set_footer(text=str(ctx.author),
                      icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

        await ctx.send(embed=em)


def setup(bot):
    bot.add_cog(Music(bot))
