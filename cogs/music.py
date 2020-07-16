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
        self.Paginator = bot.Paginator
        self.utils = bot.utils
        self.models = bot.models

    @cmd.command(name="Play", aliases=['play', 'p'], usage="play <url | query>")
    @cmd.guild_only()
    async def _play(self, ctx: cmd.Context, *, url: str = None):
        c = await self.utils.play_check(ctx, url, ctx.voice_client)
        if c:
            raise cmd.BadArgument(c)

        client = ctx.voice_client
        music = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']
        source, video = self.utils.play(ctx=ctx, url=url, cfg=music)

        if client.is_playing() or music['now_playing']:
            return await self.utils.queue(ctx, music, video)

        music['now_playing'] = self.models.NowPlaying(video).get_dict()

        np = str(type(music['now_playing']['title']))
        if np in ["<class 'tuple'>", "<class 'list'>"]:
            music['now_playing']['title'] = music['now_playing']['title'][0]

        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(music)}})

        def after_playing(err):
            self.utils.after(ctx, client, err, after_playing)

        client.play(source, after=after_playing)

        m = await ctx.send(embed=video.get_embed())
        await m.delete(delay=120)

    @cmd.command(name="Stop", aliases=['stop'], usage="stop")
    @cmd.guild_only()
    async def _stop(self, ctx: cmd.Context):
        client = ctx.voice_client
        music = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']

        if client and client.channel and ctx.author.voice and ctx.author.voice.channel:
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
        cfg = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']

        if client and client.guild and client.channel \
        and ctx.author.voice and ctx.author.voice.channel and ctx.author.voice.channel.id == client.channel.id and cfg['now_playing']:
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
    async def _volume(self, ctx: cmd.Context, value: float = None):
        client = ctx.voice_client
        cfg = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']
        em = discord.Embed(title="Volume change", description="From: `{raw}%`\nTo: `{end}%`", colour=discord.Colour.green(), timestamp=datetime.now())
        em.set_footer(text=str(ctx.author), icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

        if not value:
            em.title = "Volume"
            em.description = f"`{cfg['volume']*100}%`"
            m = await ctx.send(embed=em)
            await m.delete(delay=120)
            return

        if not ctx.author.voice or not ctx.author.voice.channel:
            raise cmd.BadArgument("To use this command: you must be in voice channel, or check bot permissions to view it")
        if value < 0.1:
            value = 0.1
            await ctx.send(f"Volume value can't be less than {value}%")
        elif value > cfg['volume_max']:
            value = cfg['volume_max']
            await ctx.send(f"Volume value can't be more than {value}")
        raw = ctx.voice_client.source.volume if client and client.source else cfg['volume']
        value /= 100
        if value == raw:
            return await ctx.send("New volume value can't equals to old")

        if client and client.source:
            ctx.voice_client.source.volume = value
        else:
            cfg['volume'] = value
        end = ctx.voice_client.source.volume if client and client.source else cfg['volume']
        self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"music": dict(cfg)}})
        em.description = em.description.format(raw=raw*100, end=end*100)

        m = await ctx.send(embed=em)
        await m.delete(delay=120)

    @cmd.command(name="Now playing", aliases=['now_playing', 'nowplaying', 'np'], usage="now_playing")
    @cmd.guild_only()
    async def _now_playing(self, ctx: cmd.Context):
        np = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']['now_playing']
        if not np:
            await ctx.send(embed=discord.Embed(description="Seems like nothing playing now"))
        raw = self.utils.parser(typ="time", start=np['start_at'], end=datetime.now())
        dur = self.utils.parser(typ="time", raw=np['duration'])
        mem = ctx.guild.get_member(int(np['req']))

        m = await ctx.send(embed=discord.Embed(colour=discord.Colour.green(),
                                description=f"Duration: `{raw}` / `{dur}`\n"
                                f"Requested by: `{str(mem or 'User not found')}` {f'[{mem.mention}]' if mem else ''}",
                                timestamp=datetime.now())
                                .set_thumbnail(url=np['thumb_url'])
                                .set_author(name=np['title'], url=np['url'], icon_url=np['channel_icon_url'])
                                .set_footer(text=str(ctx.author),
                                            icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256)))
        await m.delete(delay=120)
    
    @cmd.command(name="Queue", aliases=['queue', 'q'], usage="queue")
    @cmd.guild_only()
    async def _queue(self, ctx: cmd.Context):
        cfg = self.config.find_one({"_id": f"{ctx.guild.id}"})['music']
        np = cfg['now_playing']

        em = discord.Embed( title="Seems like queue is empty",
                            colour=discord.Colour.green(),
                            timestamp=datetime.now())
        em.set_author(name="Seems like nothing is plying now",
                        icon_url="https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg")

        em.set_thumbnail(url="https://maestroselectronics.com/wp-content/uploads/2017/12/No_Image_Available.jpg")

        if len(cfg['queue']):
            embeds = []
            desc = []
            for i in range(len(cfg['queue'])):
                n = i + 1
                j = cfg['queue'][i]
                mem = ctx.guild.get_member(int(j['req']))
                desc.append(f"`{n}` Requested by: {str(mem or 'User not found')} [{mem.mention if mem else ''}]\n[{j['title']}]({j['url']})")

                if n % 5 == 0:
                    embeds.append(discord.Embed(title="Queue list:",
                            colour=discord.Colour.green(),
                            timestamp=datetime.now(),
                            description="\n".join(desc[len(embeds)*5:]))
                            .set_thumbnail(url=np['thumb_url'])
                            .set_author(name=np['title'], url=np['url'], icon_url=np['channel_icon_url'])
                            .set_footer(text=str(ctx.author), icon_url=ctx.author.avatar_url_as(format="png",
                                                                                                static_format='png', size=256)))
            else:
                if len(desc[len(embeds)*5:]) > 0:
                    try:
                        embeds.append(discord.Embed(title="Queue list:",
                                colour=discord.Colour.green(),
                                timestamp=datetime.now(),
                                description="\n".join(desc[len(embeds)*5:]))
                                .set_thumbnail(url=np['thumb_url'])
                                .set_author(name=np['title'], url=np['url'], icon_url=np['channel_icon_url'])
                                .set_footer(text=str(ctx.author), icon_url=ctx.author.avatar_url_as(format="png",
                                                                                                    static_format='png', size=256)))
                    except:
                        pass

            if len(embeds) > 1:
                p = self.Paginator(ctx, embeds=embeds, music=cfg, cfg=self.config)
                return await p.call_controller()

            if len(embeds) == 1:
                m = await ctx.send(embed=embeds[0])
                await m.delete(delay=120)
                return

        if np:
            em.set_thumbnail(url=np['thumb_url'])
            em.set_author(name=np['title'], url=np['url'], icon_url=np['channel_icon_url'])
        em.set_footer(text=str(ctx.author), icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256))

        m = await ctx.send(embed=em)
        await m.delete(delay=120)
        

def setup(bot):
    bot.add_cog(Music(bot))
