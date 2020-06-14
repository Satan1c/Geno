from discord.ext import commands


class Sinner(commands.Converter):
    async def convert(self, ctx, argument):
        argument = await commands.MemberConverter().convert(ctx, f"{argument}")


class Moderation(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

    @commands.command(usage="ban")
    async def ban(self, ctx, user):
        return await ctx.send(type(user))


def setup(bot):
    bot.add_cog(Moderation(bot))
