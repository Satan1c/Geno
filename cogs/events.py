# -*- coding: utf-8 -*-

import re

import discord
from bot.client import Geno
from discord.ext import commands as cmd


class Events(cmd.Cog):
    def __init__(self, bot: Geno):
        self.bot = bot
        self.config = bot.servers
        self.DB = bot.DataBase

    @cmd.Cog.listener("on_guild_join")
    async def guild_join(self, guild: discord.Guild):
        await self.DB(self.bot).create_server(guild)
        print(f"\n[+]server join"
              f"\nName: {guild.name}"
              f"\nId: {guild.id}"
              f"\nMembers: {len([i.discriminator for i in guild.members if not i.bot])}"
              f"\nBots: {len([i.discriminator for i in guild.members if i.bot])}\n")

    @cmd.Cog.listener("on_guild_remove")
    async def guild_leave(self, guild: discord.Guild):
        print(f"\n[+]server leave"
              f"\nName: {guild.name}"
              f"\nId: {guild.id}\n")

    @cmd.Cog.listener("on_voice_state_update")
    async def voice_update(self, member: discord.Member, before: discord.VoiceState, after: discord.VoiceState):
        cfg = await self.config.find_one({"_id": f"{member.guild.id}"})
        cfg = cfg['music']
        if member.id == self.bot.user.id and after.channel and member.voice and not member.voice.deaf:
            try:
                await member.edit(deafen=True)
            except BaseException as err:
                print(f"\n[!]Events voice_update error:\n{err}\n")
                pass

        if member.id == member.guild.me.id and not after.channel and cfg['now_playing']:
            cfg['queue'] = []
            cfg['now_playing'] = ""
            await self.config.update_one({"_id": f"{member.guild.id}"}, {"$set": {"music": dict(cfg)}})

    @cmd.Cog.listener("on_message")
    async def message(self, message: discord.Message):
        if re.sub(r'[^@<>!&A-Za-z0-9]', r'', message.content) in [f"{str(self.bot.user)}",
                                                                  f"<@{str(self.bot.user)}>",
                                                                  f"@{str(self.bot.user)}",
                                                                  f"<@{self.bot.user.id}>"]:
            if not message.guild:
                prf = Geno.prefix
            else:
                prf = await self.config.find_one({"_id": f"{message.guild.id}"})

            ctx = cmd.Context(bot=self.bot,
                              message=message,
                              guild=message.guild,
                              send=message.channel.send,
                              prefix=prf['prefix'])
            return await self.bot.get_command("Help").callback(ctx=ctx, self=self.bot.get_cog("System"))

        if message.author.id != self.bot.user.id or not message.guild:
            return

        em = discord.Embed()
        if not message.embeds or isinstance(message.embeds[0].image.url, type(em.image.url)):
            await message.delete(delay=120)


def setup(bot):
    bot.add_cog(Events(bot))
