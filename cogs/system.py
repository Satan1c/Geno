# -*- coding: utf-8 -*-

import re
from datetime import datetime

import discord
from bot.client import geno, Geno
from discord.ext import commands as cmd

url_rx = re.compile(r'https?://(?:www\.)?.+')
checks = geno.checks


class System(cmd.Cog):
    def __init__(self, bot: Geno):
        self.bot = bot
        self.config = bot.servers
        self.commands = bot.cmds
        self.streamers = bot.streamers
        self.profile = bot.profiles
        self.utils = bot.utils
        self.twitch = bot.twitch
        self.models = bot.models
        self.Paginator = bot.Paginator
        self.EmbedGenerator = bot.EmbedGenerator
        self.bot_invite = "https://discord.com/oauth2/authorize?client_id={id}&permissions={perms}&scope=bot"
        self.supp_link = "https://discord.gg/NSkg6N9"
        self.patreon_link = "https://patreon.com/satan1c"
        self.reactions = ('⬅', '⏹', '➡')
        self.urls = ({"Bot invite:": self.bot_invite.format(id=bot.user.id, perms=536210647)},
                     {"Support server:": self.supp_link},
                     {"Patreon:": self.patreon_link},
                     {"SD.C": "https://bots.server-discord.com/648570341974736926"},
                     {"D.Boats": "https://discord.boats/bot/648570341974736926"},
                     {"Top-Bots": "https://top-bots.xyz/bot/648570341974736926"})

    @cmd.command(name="Prefix", aliases=['prf', 'set_prefix', 'set_pref', 'префикс', 'преф'],
                 usage="prefix `<prefix>`",
                 description=f"""
    prefix - любой префикс, который вам надо, не длиннее 10 символов
     примеры: `-g`, `!`, `some_awesome_prefix`
     по умолчанию: `{Geno.prefix}`

    Изменяет текущий префикс на сервере, на "prefix"
    """)
    @cmd.check(checks.is_off)
    @cmd.has_guild_permissions(manage_messages=True)
    @cmd.cooldown(1, 5, cmd.BucketType.user)
    async def prefix(self, ctx: cmd.Context, *, prefix: str = Geno.prefix):
        prefix = re.sub(r"[^-~`!@#$%^&*()_+=;№:?/{}0-9A-Za-zА-Яа-я]", r"", prefix)
        if len(prefix) > 10:
            raise cmd.BadArgument("Длина префикса не может превышать 10 символов")

        cfg = await self.config.find_one({"_id": f"{ctx.guild.id}"})
        raw = cfg['prefix']
        if str(raw) == str(prefix):
            raise cmd.BadArgument("Новый префикс не может быть как старый")

        await self.config.update_one({"_id": f"{ctx.guild.id}"}, {"$set": {"prefix": prefix}})
        await ctx.send(embed=discord.Embed(title="Смена префикса",
                                           description=f"С: `{raw}`\nНа: `{prefix}`",
                                           colour=discord.Colour.green(),
                                           timestamp=datetime.now())
                       .set_footer(text=str(ctx.author),
                                   icon_url=ctx.author.avatar_url_as(format="png", static_format='png', size=256)))

    @cmd.command(name="Role Reactions", hidden=True,
                 aliases=['rr', 'role_reactions', 'rolereactions', 'рр', 'роли_по_реакциям', 'ролипореакцим', 'рлпрк'],
                 usage="role_reactions `<\"add\" | \"remove\">` `<message id>` `<List[emoji, role id/s]>`",
                 description="""
        \"add\" | \"remove\" - текстовый параметр, для действия, добавления или уменьшения ролей по реакции с сообщения

        message id - должен быть, целым числом сообщения
         пример: `648622822889095172`

        emoji - должен быть полной реакцией, не только именем или айди(*если есть*)
         пример: 👍, <a:37:637697316932812816>

        role id/s - должен быть id роли, или их перечень разделенный пробелом
         примеры: `648571307860164618`, `648571307860164618 648571307806164618`

        пример использования команды:
         -role_reaction add 123456789012345678 👍 098765432109876543 648571307860164618; <a:37:637697316932812816> 648571307806164618
         -role_reaction message emoji role_id role_id; emoji role_id

        Добавляет "emojis" под "message", и отмечает "roles" как роли по реакциям
        """)
    @cmd.guild_only()
    @cmd.check(checks.is_off)
    @cmd.has_guild_permissions(manage_channels=True, manage_roles=True)
    @cmd.bot_has_guild_permissions(manage_channels=True, manage_roles=True)
    @cmd.cooldown(1, 5, cmd.BucketType.guild)
    async def reaction_roles(self, ctx: cmd.Context, remove: str, message: str, *, roles: str):
        cfg = await self.config.find_one({"_id": str(ctx.guild.id)})
        if remove in ["add"]:
            roles = [i.split(" ") for i in roles.split("; ")] \
                if len(re.sub(r'[^;]', r'1', roles.strip())) > 1 else roles.split(" ")

            res, roles = self.bot.utils.rroles_check(roles, ctx)
            if not res: raise cmd.BadArgument("invalid roles")

            cfg['rroles'][message] = {} if message and message not in cfg['rroles'] else cfg['rroles'][message]

            for i in roles:
                r = i[0].strip("<>").split(":")
                i[0] = r[1] if len(r) > 1 else i[0]
                cfg['rroles'][message][i[0]] = {"emoji": str(r[2]) if len(r) >= 3 and r[2] != 'None' else None,
                                                "roles": []} \
                    if i[0] and i[0] not in cfg['rroles'][message] else cfg['rroles'][message][i[0]]

                for role in i[1:]:
                    cfg['rroles'][message][i[0]]["roles"].append(role)

                m = await ctx.channel.fetch_message(int(message))

                if not m:
                    for channel in ctx.guild.text_channels:
                        m = await channel.fetch_message(int(message))
                        if not m:
                            continue
                        break

                await m.add_reaction(
                    f"<:{i[0]}:{cfg['rroles'][message][i[0]]['emoji']}>" if cfg['rroles'][message][i[0]][
                                                                                'emoji'] is not None else i[0])

            else:
                await self.config.update_one({"_id": str(ctx.guild.id)}, {"$set": dict(cfg)})

        elif remove in ["remove"]:
            roles = [i.split(" ") for i in roles.split("; ")] \
                if len(re.sub(r'[^;]', r'1', roles.strip())) > 1 else roles.split(" ")

            res, roles = self.bot.utils.rroles_check(roles, ctx)
            if not res: raise cmd.BadArgument("invalid roles")

            if message in cfg['rroles']:
                for i in roles:
                    r = i[0].strip("<>").split(":")
                    i[0] = r[1] if len(r) > 1 else i[0]
                    cfg['rroles'][message][i[0]] = {"emoji": str(r[2]) if r[2] != "None" else None, "roles": []} \
                        if i[0] and i[0] not in cfg['rroles'][message] else cfg['rroles'][message][i[0]]

                    if len(cfg['rroles'][message][i[0]]['roles']):
                        for role in i[1:]:
                            cfg['rroles'][message][i[0]]['roles'].pop(cfg['rroles'][message][i[0]]['roles'].index(role))

                        else:
                            if len(cfg['rroles'][message][i[0]]['roles']) <= 0:
                                m = await ctx.channel.fetch_message(int(message))

                                if not m:
                                    for channel in ctx.guild.text_channels:
                                        m = await channel.fetch_message(int(message))
                                        if not m:
                                            continue
                                        break

                                await m.clear_reaction(f"<:{i[0]}:{cfg['rroles'][message][i[0]]['emoji']}>" if
                                                       cfg['rroles'][message][i[0]]['emoji'] is not None else i[0])
                    else:
                        m = await ctx.channel.fetch_message(int(message))

                        if not m:
                            for channel in ctx.guild.text_channels:
                                m = await channel.fetch_message(int(message))
                                if not m:
                                    continue
                                break

                        await m.clear_reaction(f"<:{i[0]}:{i[0]['emoji']}>" if i[0]['emoji'] is not None else i[0])

                    if len(cfg['rroles'][message][i[0]]['roles']) <= 0:
                        cfg['rroles'][message].pop(i[0])
                        if len(cfg['rroles'][message]) <= 0:
                            cfg['rroles'].pop(message)

                    await self.config.update_one({"_id": str(ctx.guild.id)}, {"$set": dict(cfg)})

    @cmd.command(name="Disable", aliases=['отключить'], usage="disable `<category name | command alias>`",
                 description="""
        Category name - должен быть полным названием категории
         пример: `Music`

        Command alias - должен быть одним из вариантов использования команды
         примеры: `p`, `ban`, `srv`

        Отключает все команды в "category" или команду "command" на сервере
        """)
    @cmd.guild_only()
    @cmd.check(checks.is_off)
    @cmd.has_guild_permissions(manage_guild=True)
    @cmd.cooldown(1, 5, cmd.BucketType.user)
    async def disable_command_or_category(self, ctx: cmd.Context, *, target: str):
        if not target:
            raise cmd.BadArgument("Give name of category or command alias")
        target = target.split(" ")
        target = [i.lower() for i in target]

        cfg = await self.commands.find_one({"_id": f"{ctx.guild.id}"})

        if not cfg:
            data = self.models.Commands(ctx.guild).get_dict()
            await self.commands.insert_one(dict(data))
            cfg = data

        cogs = [i for i in self.bot.cogs if i.lower() in target]

        cmds = [i.name for j in self.bot.cogs for i in self.bot.cogs[j].walk_commands()
                if not i.hidden and j != "Enable" and len([v for v in target if v in i.aliases]) > 0]

        if not cmds and not cogs:
            raise cmd.BadArgument('Nothing found!')

        if cogs:
            if cogs[0] in cfg['cogs']:
                return await ctx.send("This category already disabled")

            cfg['cogs'].append(cogs[0])
            res = f"Category: {cogs[0]}"
        else:
            if cmds[0] in cfg['commands']:
                return await ctx.send("This command already disabled")

            cfg['commands'].append(cmds[0])
            res = f"Command: {cmds[0]}"

        await self.commands.update_one({"_id": f"{ctx.guild.id}"}, {"$set": dict(cfg)})
        await ctx.send(embed=discord.Embed(title="Выключена:",
                                           description=res,
                                           colour=discord.Colour.green(),
                                           timestamp=datetime.now()))

    @cmd.command(name="Enable", aliases=['включить'], usage="enable `<category name | command alias>`",
                 description="""
        Category name - должен быть полным названием категории
         пример: `Music`

        Command alias - должен быть одним из вариантов использования команды
         примеры: `p`, `ban`, `srv`

        Включает все команды в "category" или команду "command" на сервере
        """)
    @cmd.guild_only()
    @cmd.has_guild_permissions(manage_guild=True)
    @cmd.cooldown(1, 5, cmd.BucketType.user)
    async def enable_command_or_category(self, ctx: cmd.Context, *, target: str):
        if not target:
            raise cmd.BadArgument("Укажите имя категории или \"использование\" команды")

        target = target.split(" ")
        target = [i.lower() for i in target]

        cfg = await self.commands.find_one({"_id": f"{ctx.guild.id}"})

        if not cfg:
            raise cmd.BadArgument("Все категории/команды - включены")

        cogs = [i for i in self.bot.cogs if i.lower() in target]

        cmds = [i.name for j in self.bot.cogs for i in self.bot.cogs[j].walk_commands()
                if not i.hidden and j != "Enable" and len([v for v in target if v in i.aliases]) > 0]

        if not cmds and not cogs:
            raise cmd.BadArgument('Ничего не найдено')

        if cogs:
            if cogs[0] not in cfg['cogs']:
                return await ctx.send("Эта категория уже включена")

            res = cfg['cogs'].pop([i for i in range(len(cfg['cogs'])) if cfg['cogs'][i].lower() == cogs[0].lower()][0])
            res = f"Категория: {res}"
        else:
            if cmds[0] not in cfg['commands']:
                return await ctx.send("Эта команда уже включена")

            res = cfg['commands'].pop(
                [i for i in range(len(cfg['commands'])) if cfg['commands'][i].lower() == cmds[0].lower()][0])
            res = f"Соманда: {res}"

        await self.commands.update_one({"_id": f"{ctx.guild.id}"}, {"$set": dict(cfg)})
        await ctx.send(embed=discord.Embed(title="Включена:",
                                           description=res,
                                           colour=discord.Colour.green(),
                                           timestamp=datetime.now()))


def setup(bot):
    bot.add_cog(System(bot))
