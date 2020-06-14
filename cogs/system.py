import discord
from discord.ext import commands


class System(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

    @commands.command(hidden=True)
    async def help(self, ctx):
        em = discord.Embed(color=0x0390fc, title='Помощь по командам')
        description = []

        for cog in self.bot.cogs:
            if cog == "Jishaku":
                continue
            listt = list(self.bot.cogs[cog].walk_commands())
            cmds = [f"{x + 1}. `{listt[x].usage}`" for x in range(len(listt)) if not listt[x].hidden]

            if len(cmds) == 0:
                continue

            cmds = "\n".join(cmds)
            description.append(f'**{cog}**\n{cmds}\n')

        em.description = '\n'.join(description)

        return await ctx.send(embed=em)


def setup(bot):
    bot.add_cog(System(bot))
