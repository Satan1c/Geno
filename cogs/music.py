# -*- coding: utf-8 -*-

import asyncio

from discord.ext import commands as cmd


class Music(cmd.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.config = bot.servers
        self.queue = []
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
        req = {"tag": str(ctx.author),
               "ava": str(ctx.author.avatar_url_as(format="png", static_format='png', size=256))}

        if client.is_playing() and not music['now_playing']:
            music['queue'].append({"req": req, "url": url})
            self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})
            return await ctx.send(f"Queue updated: {video.video_url}")

        music['now_playing'] = {"req": req, "url": video.video_url}

        await ctx.send(f"Queue updated: {video.video_url}")

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
                music['now_playing'] = q['url']

                self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})
                source, video = self.utils.play(req=q['req'], url=q['url'])

                asyncio.run_coroutine_threadsafe(ctx.send(embed=video.get_embed()), self.bot.loop)

                client.play(source, after=after_playing)

        client.play(source, after=after_playing)

        return await ctx.send(embed=video.get_embed())

    @cmd.command(name="Stop", aliases=['stop'], usage="stop")
    @cmd.guild_only()
    async def _stop(self, ctx: cmd.Context):
        client = ctx.voice_client
        music = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']

        if client and client.guild and client.channel and client.is_playing():
            music['queue'] = []
            self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})

            return client.stop()

    @cmd.command(name="Leave", aliases=['leave', 'l'], usage="leave")
    @cmd.guild_only()
    async def _leave(self, ctx: cmd.Context):
        client = ctx.voice_client
        music = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']

        if client and client.guild and client.channel:
            music['queue'] = []
            self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})
            return client.stop()

    @cmd.command(name="Skip", aliases=['skip', 's'], usage="skip")
    @cmd.guild_only()
    async def _skip(self, ctx: cmd.Context):
        client = ctx.voice_client

        if client and client.guild and client.channel and client.is_playing():
            return client.stop()

    @cmd.command(name="Pause", aliases=['pause'], usage="pause")
    @cmd.guild_only()
    async def _pause(self, ctx: cmd.Context):
        client = ctx.voice_client

        if client and client.guild and client.channel:

            if client.is_playing():
                return client.pause()

            if client.is_paused():
                return client.resume()


def setup(bot):
    bot.add_cog(Music(bot))
