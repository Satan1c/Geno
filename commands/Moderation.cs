using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Geno.commands
{
    public class Moderation : BaseCommandModule
    {
        public Moderation()
        {
            var names = new string[]
            {
                "ban",
                "clearwarns"
            };

            foreach (var i in names)
            {
                Bot.help[i] = "Moderation";
            }
        }

        [Command("clearwarns"),
            Aliases(new string[] { "cw" }),
            utils.Utils.RequireUserPermissions(Permissions.ManageMessages),
            Description("Очищает локальные предупреждения пользователя, пользователь должен быть на сервере\n" +
            ":-:\n" +
            "Использование: `{0}clearWarns <упоминание | id | ник>`"),
            Cooldown(1, Bot.smallCD, CooldownBucketType.Guild)]
        public async Task ClearWarns(CommandContext ctx, DiscordMember mem)
        {
            var profile = await utils.Utils.GetProfile(mem);

            if (profile.localWarns[ctx.Guild.Id.ToString()] != 0)
            {
                profile.localWarns[ctx.Guild.Id.ToString()] = 0;

                var coll = Bot.mongo.GetDatabase("users").GetCollection<models.User>("profiles");
                await coll.ReplaceOneAsync((filter) => filter._id == profile._id, profile).ConfigureAwait(false);
            }
        }

        [Command("ban"),
            Aliases(new string[] { "b" }),
            utils.Utils.RequireUserPermissions(Permissions.BanMembers),
            utils.Utils.RequireBotPermissions(Permissions.BanMembers),
            Description("Банит пользователя на сервере, чтобы забанить не обязательно нахождение пользователя на сервере\n" +
            "Требует права для выполнения:\n" +
            "- пользователь - BanMembers\n" +
            "- бот - BanMembers\n" +
            ":-:\n" +
            "Использование: `{0}ban` `<упоминание | id | ник>` `[причина]`\n" +
            "\nДля Бана участника, можно указать:\n" +
            "- его упоминание - {3},\n" +
            "- или его id - {1},\n" +
            "- или его ник(*иногда может не срабатывать*) - {2}\n" +
            "\nДля Бана пользователя, можно указать:\n" +
            "- или его id - {1},\n\n" +
            "Пример использования: `{0}ban` `{1}` `громко ест печеньки`"),
            Cooldown(1, Bot.smallCD, CooldownBucketType.User)]
        public async Task Ban(CommandContext ctx, ulong memID, [RemainingText] string reason)
        {
            if (memID == ctx.Member.Id || memID == ctx.Guild.CurrentMember.Id)
                throw new ArgumentException();
            try
            {
                var user = await ctx.Channel.Guild.GetMemberAsync(memID);
                await Ban(ctx, user, reason);
            }
            catch
            {
                try
                {
                    DiscordUser user = await ctx.Client.GetUserAsync(memID);
                    await ctx.Channel.Guild.BanMemberAsync(user.Id, reason: reason);
                }
                catch
                {

                }
            }
        }

        [Command("ban")]
        public async Task Ban(CommandContext ctx, DiscordMember mem, [RemainingText] string reason)
        {
            if (mem.Id == ctx.Member.Id || mem.Id == ctx.Guild.CurrentMember.Id)
                throw new ArgumentException();
            await ctx.Channel.Guild.BanMemberAsync(mem.Id, reason: reason);
        }
    }
}