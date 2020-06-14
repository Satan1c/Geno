import math

import discord
import youtube_dl
from discord.ext import commands

from internal.utils import YTDL_OPTS


class Music(commands.Cog):
    def __init__(self, bot):
        self.video = bot.video
        self.bot = bot
        self.config = bot.db.servers.music
        self.utils = bot.utils

    @commands.command(aliases=["stop", "l", "leave"], usage="leave")
    @commands.guild_only()
    async def _leave(self, ctx: commands.Context):
        if await self.utils.not_audio_or_voice(ctx):
            return

        client = ctx.guild.voice_client

        if client and client.channel:
            if client.is_playing() or client.is_paused():
                self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"queue": [], "now_playing": ""}})
                return client.stop()

            await client.disconnect()

        else:
            return await ctx.send("Not in a voice channel.")

    @commands.command(aliases=["join", "j"], usage="join")
    @commands.guild_only()
    async def _join(self, ctx):
        if await self.utils.not_audio_or_voice(ctx):
            return await ctx.send("You need to be in a voice channel to do that.")

        client = ctx.voice_client
        channel = ctx.author.voice.channel

        if not client or not client.channel:
            await channel.connect()

        else:
            return await ctx.send("Already in a voice channel.")

    @commands.command(aliases=["resume", "pause"], usage="pause")
    @commands.guild_only()
    async def _pause(self, ctx):
        if await self.utils.not_audio_or_voice(ctx):
            return

        client = ctx.voice_client
        self.utils.pause_audio(client)

    @commands.command(aliases=["vol", "v"], usage="volume")
    @commands.guild_only()
    async def volume(self, ctx, volume: int):
        if await self.utils.not_audio_or_voice(ctx):
            return

        if volume != 0 and volume > 1 and int(volume) == 0:
            volume *= 100

        config = self.config.find_one({"_id": f"{ctx.guild.id}"})

        if volume < 0:
            volume = 0
            await ctx.send("Volume must be more or equal `0`")

        max_vol = config["max_volume"]
        if max_vol > -1:
            if volume > max_vol:
                volume = max_vol
                await ctx.send(f"Volume can't be more than max(`{max_vol}`)")

        client = ctx.voice_client

        volume = float(volume) / 100.0
        client.source.volume = volume
        await ctx.send(f"Volume changed to {volume}")

        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"volume": volume}})

    @commands.command(usage="skip")
    @commands.guild_only()
    async def skip(self, ctx):
        if await self.utils.not_audio_or_voice(ctx):
            return

        client = ctx.voice_client
        config = self.config.find_one({"_id": f"{ctx.guild.id}"})

        if config["vote_skip"]:

            channel = client.channel
            self.utils.vote_skip(channel, ctx.author)

            users_in_channel = len([member for member in channel.members if not member.bot])  # don't count bots
            required_votes = math.ceil(config["vote_skip_ratio"] * users_in_channel)

            await ctx.send(f"{ctx.author.mention} voted to skip ({config['skip_votes']}/{required_votes} votes)")

        else:
            client.stop()

    @commands.command(aliases=["np", "nowplaying"], usage="nowplaying")
    @commands.guild_only()
    async def now_playing(self, ctx):
        if await self.utils.not_audio_or_voice(ctx):
            return

        config = self.config.find_one({"_id": f"{ctx.guild.id}"})
        em = await self.utils.queue_embed(config, one=True)

        await ctx.send(embed=em)

    @commands.command(aliases=["q"], usage="queue")
    @commands.guild_only()
    async def queue(self, ctx):

        config = self.config.find_one({"_id": f"{ctx.guild.id}"})
        em = await self.utils.queue_embed(config)

        msg = await ctx.send(embed=em)

        await self.utils.update_last(ctx, config, msg)

    @commands.command(aliases=["cq", "clearqueue"], usage="clearqueue")
    @commands.guild_only()
    async def clear_queue(self, ctx):

        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"queue": []}})

    @commands.command(aliases=["jq", "jumpqueue"], usage="jumpqueue")
    @commands.guild_only()
    async def jump_queue(self, ctx, index: int):
        if await self.utils.not_audio_or_voice(ctx):
            return

        config = self.config.find_one({"_id": f"{ctx.guild.id}"})

        if len(config['queue']) >= index >= 1:
            pl = config['queue'][index:]

            self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"queue": pl}})
            config = self.config.find_one({"_id": f"{ctx.guild.id}"})
            em = await self.utils.queue_embed(config)

            await ctx.send(embed=em)
            client = ctx.voice_client
            return client.stop()

        else:
            await ctx.send("You must use a valid index.")

    @commands.command(aliases=["plplay", "plp", "playlistplay"], usage="playlistplay")
    @commands.guild_only()
    async def playlist_play(self, ctx, *, name):
        if await self.utils.not_audio_or_voice(ctx):
            return await ctx.send("You need to be in a voice channel to do that.")
        try:

            config = self.config.find_one({"_id": f"{ctx.guild.id}"})
            await self.utils.make_queue(config, name)

            video = self.video(config['queue'][0]['url'], {"tag": f"{ctx.author.name}#{ctx.author.discriminator}",
                                                           "ava": f"{ctx.author.avatar_url_as(format='png', static_format='png', size=512)}"})
            await self.utils.play(ctx=ctx, video=video)

        except youtube_dl.DownloadError:
            return await ctx.send("There was an error downloading your video, sorry.")

    @commands.command(aliases=["p", 'play'], usage="play")
    @commands.guild_only()
    async def _play(self, ctx, *, url: str):
        print(url)
        if await self.utils.not_audio_or_voice(ctx):
            return await ctx.send("You need to be in a voice channel to do that.")
        req = {"tag": f"{ctx.author.name}#{ctx.author.discriminator}",
               "ava": f"{ctx.author.avatar_url_as(format='png', static_format='png', size=512)}"}

        if not url:
            config = self.config.find_one({"_id": f"{ctx.guild.id}"})
            if not config['queue']:
                return await ctx.send(embed=discord.Embed(title="Queue is empty"))

            client = ctx.voice_client or await ctx.author.voice.channel.connect()
            cfg = config['queue'][0]
            video = self.video(cfg['url'], cfg['req'])

            return self.utils.play_song(client, video, ctx)

        try:
            print(0)
            video = self.video(url, req)
            print(0.1)
            await self.utils.play(ctx=ctx, video=video)

        except youtube_dl.DownloadError:
            return await ctx.send("There was an error downloading your video, sorry.")

    @commands.command(aliases=['createplaylist', 'crpl'], usage="createplaylist")
    @commands.guild_only()
    async def create_playlist(self, ctx, *, url, name):
        try:

            with youtube_dl.YoutubeDL(YTDL_OPTS) as ydl:
                info = ydl.extract_info(url, download=False)

                if "_type" in info and info["_type"] == "playlist":
                    r = []
                    for i in info["entries"]:
                        r.append(i['url'])
                    else:
                        self.config.update_one({"_id": ctx.guild.id}, {"$set": {f"{name}": r}})

                else:
                    await ctx.send("It is not playlist")

        except youtube_dl.DownloadError:
            return await ctx.send("There was an error downloading your video, sorry.")

    @commands.Cog.listener()
    async def on_reaction_add(self, reaction, user):
        message = reaction.message
        client = message.guild.voice_client
        config = self.config.find_one({"_id": f"{message.author.guild.id}"})

        if message.id != config['last']['message']:
            return

        if not user.bot and user.voice.channel.id == client.channel.id:
            await message.remove_reaction(reaction, user)

            if message.guild and client:
                user_in_channel = user.voice and user.voice.channel and client and client.channel and user.voice.channel == client.channel

                if reaction.emoji == "⏹":
                    self.config.update_one({"_id": f"{message.author.guild.id}"}, {"$set": {"queue": []}})
                    await message.delete()
                    client.stop()

                elif reaction.emoji == "⏯":
                    self.utils.pause_audio(client)

                elif reaction.emoji == "⏭" and user_in_channel:
                    voice_channel = client.channel

                    if config['skip_vote']:
                        users_in_channel = len([member for member in voice_channel.members if not member.bot])
                        required_votes = math.ceil(config["vote_skip_ratio"] * users_in_channel)
                        self.utils.vote_skip(voice_channel, user)

                        await message.channel.send(
                            f"{user.mention} voted to skip ({config['skip_votes']}/{required_votes} votes)")

                    else:
                        client.stop()


def setup(bot):
    bot.add_cog(Music(bot))
