# -*- coding: utf-8 -*-

import asyncio

import discord
from discord.ext import commands as cmd
from datetime import datetime


class Music(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.config = bot.servers
        self.Video = bot.Video
        self.utils = bot.utils

    @cmd.command(name="Play", aliases=['play', 'p'], usage="play <url | query>")
    @cmd.guild_only()
    async def _play(self, ctx: cmd.Context, *, url: str = None):
        client = ctx.voice_client

        if not url:
            return await ctx.send("Please give video url or title")
        if not ctx.author.voice or not ctx.author.voice.channel:
            raise cmd.BadArgument("To use this command: you must be in voice channel, or check bot permissions to "
                                  "view it")
        if not client or not client.channel:
            client = await ctx.author.voice.channel.connect()

        source, video = self.utils.play(ctx=ctx, url=url)

        music = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']
        req = f"{ctx.author.id}"

        if client.is_playing():
            em = video.get_embed()
            music['queue'].append({"req": req, "url": video.video_url})
            self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})
            return await ctx.send(embed=discord.Embed(title=video.title, url=video.video_url, colour=discord.Colour.green())
                                    .set_thumbnail(url=em.image.url)
                                    .set_author(name=f"{str(ctx.author)} add to queue:",
                                            icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256)))

        music['now_playing'] = {"req": req, "url": video.video_url}
        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})

        def after_playing(err):
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
                music['now_playing'] = {"req": q['req'], "url": q['url']}

                self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})
                source, video = self.utils.play(url=q['url'], req=self.bot.get_user(int(q['req'])))

                asyncio.run_coroutine_threadsafe(ctx.send(embed=video.get_embed()), self.bot.loop)

                client.play(source, after=after_playing)

        client.play(source, after=after_playing)

        return await ctx.send(embed=video.get_embed())

    @cmd.command(name="Stop", aliases=['stop'], usage="stop")
    @cmd.guild_only()
    async def _stop(self, ctx: cmd.Context):
        client = ctx.voice_client
        music = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']

        if client and client.guild and client.channel and ctx.author.voice and ctx.author.voice.channel and client.is_playing():
            music['queue'] = []
            self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})

            return client.stop()

    @cmd.command(name="Leave", aliases=['leave', 'l'], usage="leave")
    @cmd.guild_only()
    async def _leave(self, ctx: cmd.Context):
        client = ctx.voice_client

        if client and client.guild and client.channel and ctx.author.voice and ctx.author.voice.channel:
            if client.is_playing():
                client.stop()
            return await client.disconnect()

    @cmd.command(name="Skip", aliases=['skip', 's'], usage="skip")
    @cmd.guild_only()
    async def _skip(self, ctx: cmd.Context):
        client = ctx.voice_client

        if client and client.guild and client.channel \
        and ctx.author.voice and ctx.author.voice.channel and ctx.author.voice.channel.id == client.channel.id and client.is_playing():
            return client.stop()

    @cmd.command(name="Pause", aliases=['pause', 'resume'], usage="pause")
    @cmd.guild_only()
    async def _pause(self, ctx: cmd.Context):
        client = ctx.voice_client

        if client and client.guild and client.channel \
        and ctx.author.voice and ctx.author.voice.channel and ctx.author.voice.channel.id == client.channel.id:

            if client.is_playing():
                return client.pause()

            if client.is_paused():
                return client.resume()

    
    @cmd.command(name="Volume", aliases=['volume', 'v'], usage="volume <value>")
    @cmd.guild_only()
    async def _volume(self, ctx: cmd.Context, value: float = 0.5):
        client = ctx.voice_client
        cfg = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']

        em = discord.Embed(title="Volume change",
        description="From: `{raw}%`\nTo: `{end}%`",
        colour=discord.Colour.green(),
        timestamp=datetime.now())

        em.set_footer(text=str(ctx.author), icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

        if not ctx.author.voice or not ctx.author.voice.channel:
            raise cmd.BadArgument("To use this command: you must be in voice channel, or check bot permissions to "
                                  "view it")
                                  
        if value < 0.1:
            value = 0.1
            await ctx.send("Volume value can't be less than 0.1%")

        elif value > cfg['volume_max']:
            value = cfg['volume_max']
            await ctx.send(f"Volume value can't be more than {cfg['volume_max']}")

        raw = ctx.voice_client.source.volume if client and client.source else cfg['volume']
        value /= 100

        if value == raw:
            return await ctx.send("New volume value can't equals to old")
        if client and client.source:
            ctx.voice_client.source.volume = value
        else:
            cfg['volume'] = value

        end = ctx.voice_client.source.volume if client and client.source else cfg['volume']

        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": dict(cfg)})
        em.description = em.description.format(raw=raw*100, end=end*100)

        await ctx.send(embed=em)

    @cmd.command(name="Now playing", aliases=['now_playing', 'nowplaying', 'np'], usage="now_playing")
    @cmd.guild_only()
    async def _now_playing(self, ctx: cmd.Context):
        cfg = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']
        source, video = self.utils.play(req=self.bot.get_user(int(cfg['now_playing']['req'])), url=cfg['now_playing']['url'])
        em = video.get_embed()

        return await ctx.send(embed=discord.Embed(colour=discord.Colour.green(), description=f"Requested by: {video.req['tag']}", timestamp=datetime.now())
                                .set_thumbnail(url=em.image.url)
                                .set_author(name=em.title, url=em.url, icon_url=em.author.icon_url)
                                .set_footer(text=str(ctx.author), icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256)))

    @cmd.command(name="Queue", aliases=['queue', 'q'], usage="queue")
    @cmd.guild_only()
    async def _queue(self, ctx: cmd.Context):
        cfg = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']
        em = discord.Embed(colour=discord.Colour.green(),
        timestamp=datetime.now())
        em.set_footer(text=str(ctx.author), icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))
        if len(cfg['queue']) > 0:
            em.title = "Queue list:"

        for i in cfg['queue']:
            source, video = self.utils.play(req=self.bot.get_user(int(i['req'])), url=i['url'])
            em.add_field(name=f"Requested by: {video.req['tag']}", value=f"[{video.title}]({video.video_url})", inline=False)
        else:
            source, video = self.utils.play(req=self.bot.get_user(int(cfg['now_playing']['req'])), url=cfg['now_playing']['url'])
            raw = video.get_embed()
            em.set_thumbnail(url=raw.image.url)
            em.set_author(name=raw.title, url=raw.url, icon_url=raw.author.icon_url)
            em.set_footer(text=video.req['tag'], icon_url=video.req['ava'])

        await ctx.send(embed=em)

def setup(bot):
    bot.add_cog(Music(bot))
