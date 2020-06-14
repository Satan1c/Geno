import asyncio

import discord
import youtube_dl
from discord.ext import commands

YTDL_OPTS = {
    "default_search": "ytsearch",
    "format": "bestaudio/best",
    "quiet": True,
    "extract_flat": "in_queue"
}


class Video:
    def __init__(self, url_or_search, requested_by):

        with youtube_dl.YoutubeDL(YTDL_OPTS):
            video = self._get_info(url_or_search)
            video_format = video["formats"][0]
            self.stream_url = video_format["url"]
            self.video_url = video["webpage_url"]
            self.title = video["title"]
            self.uploader = video["uploader"] if "uploader" in video else ""
            self.thumbnail = video["thumbnail"] if "thumbnail" in video else None
            self.req = requested_by

    def _get_info(self, video_url):
        with youtube_dl.YoutubeDL(YTDL_OPTS) as ydl:
            info = ydl.extract_info(video_url, download=False)

            if "_type" in info and info["_type"] == "playlist":
                return self._get_info(info["entries"][0]['webpage_url'])  # get info for first video

            return info

    def get_embed(self):
        embed = discord.Embed(title=self.title, description=self.uploader, url=self.video_url)

        embed.set_footer(text=f"Requested by {self.req['tag']}", icon_url=self.req['ava'])

        if self.thumbnail:
            embed.set_image(url=self.thumbnail)

        return embed


class Utils:
    def __init__(self, bot):
        self.bot = bot
        self.config = bot.db.servers.music

    def pause_audio(self, client: discord.VoiceClient = None):
        if client.is_paused():
            client.resume()

        else:
            client.pause()

    async def queue_embed(self, config: dict = None, one: bool = False):
        nm = config['now_playing']
        if not config['queue']:
            return discord.Embed(title="Queue is empty")

        if one:
            c = config['queue'][0]
            video = Video(c['url'], c['req'])
            return video.get_embed()

        if nm == "" or not nm:
            em = discord.Embed(title="queue")
            r = []
            n = 0
            for i in config['queue']:
                video = Video(i['url'], i['req'])
                r.append({"req": video.req, "title": video.title, "url": video.video_url, "icon": video.thumbnail})
                em.add_field(
                    name=f"Requested by: {video.req['tag']}",
                    value=f"{n}. __{video.title}__" if video.video_url == config['queue'][0][
                        'url'] else f"{n}. {video.title}", inline=False)

                n += 1

            else:
                em.set_author(name=r[0]['title'], url=r[0]['url'], icon_url=r[0]['req']['ava'])
                em.set_thumbnail(url=r[0]['icon'])
                return em

        else:
            em = discord.Embed(title="playlist")
            r = []
            n = 0
            for i in config['playlist'][f"{nm}"]:
                video = Video(i['url'], i['req'])
                r.append({"req": video.req, "title": video.title, "url": video.video_url, "icon": video.thumbnail})
                em.add_field(name=f"{n}. {video.title}", value=video.req, inline=False)

                n += 1

            else:
                em.set_author(name=r[0]['title'], url=r[0]['url'], icon_url=r[0]['icon'])
                return em

    async def not_audio_or_voice(self, ctx: commands.Context = None):
        if ctx.author.voice and ctx.author.voice.channel:
            return False

        return True

    async def add_reaction_controls(self, message: discord.Message = None):
        controls = ["⏹", "⏯", "⏭"]

        for control in controls:
            await message.add_reaction(control)

    async def update_last(self, ctx: commands.Context = None, cfg: dict = None, message: discord.Message = None):
        msg = None if not cfg['last']['message'] else await ctx.channel.fetch_message(int(cfg['last']['message']))

        if msg:
            await msg.delete()
            await self.add_reaction_controls(message)

        self.config.update_one({"_id": f"{ctx.guild.id}"}, {
            "$set": {"last": {"message": f"{message.id}", "channel": f"{message.channel.id}",
                              "author": f"{ctx.author.id}"}}})

    async def edit_last(self, cfg: dict = None, ctx: commands.Context = None, embed: discord.Embed = None):
        msg = await ctx.channel.fetch_message(int(cfg['last']['message']))

        if msg:
            await msg.edit(embed=embed)
            await self.add_reaction_controls(embed)

        self.config.update_one({"_id": f"{ctx.guild.id}"}, {
            "$set": {"last": {"message": f"{msg.id}", "channel": f"{msg.channel.id}", "author": f"{ctx.author.id}"}}})

    async def make_queue(self, config: dict = None, name: str = None):
        q = config['queue']
        for i in config[name]:
            q.append({"url": i['url'], "req": i['req']})

        else:
            self.config.update_one({"_id": config['_id']}, {"$set": {"queue": q}})

    def vote_skip(self, channel: discord.VoiceChannel = None, member: discord.Member = None):
        config = self.config.find_one({"_id": f"{member.guild.id}"})
        votes = config['skip_votes'] + 1
        self.config.update_one({"_id": f"{member.guild.id}"}, {"$set": {"skip_votes": votes}})

        users_in_channel = len([member for member in channel.members if not member.bot])
        config = self.config.find_one({"_id": f"{member.guild.id}"})

        if (float(config['skip_votes']) / users_in_channel) >= config["vote_skip_ratio"]:
            channel.guild.voice_client.stop()

    def play_song(self, client: discord.VoiceClient = None, song: Video = None, ctx: commands.Context = None):
        print(4)
        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {'now_playing': "", 'skip_votes': 0}})
        config = self.config.find_one({"_id": f"{ctx.guild.id}"})

        source = discord.PCMVolumeTransformer(
            discord.FFmpegPCMAudio(song.stream_url,
                                   before_options="-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5",
                                   options='-vn'),
            volume=config['volume'])

        def after_playing(err):
            cfg = self.config.find_one({"_id": f"{ctx.guild.id}"})
            if len(cfg['queue']) > 1:
                next_song = cfg['queue'][1]
                next_song = Video(next_song['url'], next_song['req'])
                self.play_song(client, next_song, ctx)
                asyncio.run_coroutine_threadsafe(self.edit_last(cfg, ctx, song.get_embed()), self.bot.loop)

            else:
                self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"queue": []}})
                asyncio.run_coroutine_threadsafe(client.disconnect(), self.bot.loop)

            if err:
                return

        client.play(source, after=after_playing)

    async def play(self, ctx: commands.Context = None, video: Video = None):
        client = ctx.guild.voice_client
        channel = ctx.author.voice.channel

        config = self.config.find_one({"_id": f"{ctx.guild.id}"})
        cfg = config['queue']
        mus = {"url": video.video_url, "req": video.req}

        cfg.append(mus)
        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"queue": list(cfg)}})

        if not client and client.channel:
            client = await channel.connect()

        self.play_song(client, video, ctx)

        message = await ctx.send("Added to queue.", embed=video.get_embed())

        await self.update_last(ctx=ctx, cfg=config, message=message)
