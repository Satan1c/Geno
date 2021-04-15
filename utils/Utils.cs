using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Geno.utils
{
    internal enum AntiSpamMode
    {
        disabled,
        messageCount,
        charsCount
    }

    internal enum DefenceLevel
    {
        soft,
        hard,
        berserker
    }

    internal class Utils
    {
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class RequireUserPermissionsAttribute : CheckBaseAttribute
        {
            public Permissions Permissions { get; }
            public bool IgnoreDms { get; } = true;

            public RequireUserPermissionsAttribute(Permissions permissions)
            {
                this.Permissions = permissions;
            }

            public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
            {
                if (ctx.Guild == null)
                    return Task.FromResult(this.IgnoreDms);

                var usr = ctx.Member;
                if (usr == null)
                    return Task.FromResult(false);

                if (usr.Id == ctx.Guild.OwnerId || ctx.Client.CurrentApplication.Owners.Any(x => x.Id == usr.Id))
                    return Task.FromResult(true);

                var pusr = ctx.Channel.PermissionsFor(usr);

                if ((pusr & Permissions.Administrator) != 0)
                    return Task.FromResult(true);

                if ((pusr & this.Permissions) == this.Permissions)
                    return Task.FromResult(true);

                return Task.FromResult(false);
            }
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class RequireBotPermissionsAttribute : CheckBaseAttribute
        {
            public Permissions Permissions { get; }
            public bool IgnoreDms { get; } = true;

            public RequireBotPermissionsAttribute(Permissions permissions)
            {
                this.Permissions = permissions;
            }

            public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
            {
                if (ctx.Guild == null)
                    return this.IgnoreDms;

                var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id).ConfigureAwait(false);
                if (bot == null)
                    return false;

                if (bot.Id == ctx.Guild.OwnerId)
                    return true;

                var pbot = ctx.Channel.PermissionsFor(bot);

                if ((pbot & Permissions.Administrator) != 0)
                    return true;

                if ((pbot & this.Permissions) == this.Permissions)
                    return true;

                return false;
            }
        }

        #region RequirePermissions
        /*
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class RequirePermissionsAttribute : CheckBaseAttribute
        {
            public Permissions Permissions { get; }
            public bool IgnoreDms { get; } = true;

            public RequirePermissionsAttribute(Permissions permissions)
            {
                this.Permissions = permissions;
            }

            public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
            {
                if (ctx.Guild == null)
                    return this.IgnoreDms;

                var usr = ctx.Member;
                if (usr == null)
                    return false;
                var pusr = ctx.Channel.PermissionsFor(usr);

                var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id).ConfigureAwait(false);
                if (bot == null)
                    return false;
                var pbot = ctx.Channel.PermissionsFor(bot);

                var usrok = ctx.Guild.OwnerId == usr.Id;
                var botok = ctx.Guild.OwnerId == bot.Id;

                if (!usrok)
                    usrok = (pusr & Permissions.Administrator) != 0 || (pusr & this.Permissions) == this.Permissions;

                if (!botok)
                    botok = (pbot & Permissions.Administrator) != 0 || (pbot & this.Permissions) == this.Permissions;

                return usrok && botok;
            }
        }
        */
        #endregion

        public static async Task<bool> Rename(DiscordMember member)
        {
            var res = false;

            var clearNick = utils.Utils.TrimNonAscii(member.Username);

            if (clearNick.Length <= 2)
                clearNick = "Name";
            try
            {
                if (clearNick != member.Username || member.DisplayName != clearNick)
                {
                    await member.ModifyAsync((mem) => { mem.Nickname = clearNick; }).ConfigureAwait(false);
                    res = true;
                }
            }
            catch
            {
            }

            return res;
        }

        public static async Task Rename(IEnumerable<DiscordMember> members)
        {
            foreach (var i in members)
            {
                _ = await Rename(i).ConfigureAwait(false);
            }
        }

        public static bool HasPermissions(DiscordMember mem, Permissions perms, bool any = false, DiscordClient client = null)
        {
            if (client != null && client.CurrentApplication.Owners.Any(x => x.Id == mem.Id))
                return true;

            if (!any)
                return mem.Roles.Any((x) => (x.Permissions & Permissions.Administrator) != 0 || (x.Permissions & perms) == perms);
            
            return mem.Roles.Any((x) => (x.Permissions & Permissions.Administrator) != 0 || (x.Permissions & perms) != 0);
        }

        public static bool IsMember(DiscordMessage msg, DiscordMember member)
        {
            var res = false;
            var arg = msg.Content.Split(" ").Length >= 2 ? msg.Content.Split(" ")[1] : "";

            if (arg != "")
            {
                if (arg == member.DisplayName ||
                    arg == member.Username ||
                    arg == member.Id.ToString() ||
                    arg == member.Mention ||
                    arg == $"<@{member.Id}>")
                {
                    res = true;
                }
            }

            return res;
        }

        public static string BucketErrorString(CooldownBucketType bucket, string treshold)
        {
            var res = $"Подождите {MathF.Round(float.Parse(treshold.Substring(0, treshold.IndexOf(",") + 4)), 1)}s перед использованием команды";
            if (bucket.Equals(CooldownBucketType.Channel))
            {
                res += " в этом канале";
            }
            else if (bucket.Equals(CooldownBucketType.Guild))
            {
                res += " на этом сервере";
            }
            else if (bucket.Equals(CooldownBucketType.User) || bucket.Equals(CooldownBucketType.Global))
            {
                res += "";
            }
            return res;
        }

        public static string BucketErrorString(CooldownBucketType bucket, double treshold)
        {
            return BucketErrorString(bucket, treshold.ToString());
        }

        public static string BucketErrorString(CooldownBucketType bucket, float treshold)
        {
            return BucketErrorString(bucket, treshold.ToString());
        }

        public static async Task<bool> Punishment(DiscordGuild guild, DiscordUser user)
        {
            return await Punishment(guild, user.Id);
        }

        public static async Task<bool> Punishment(DiscordGuild guild, DiscordMember user)
        {
            var res = false;
            var cfg = await utils.Utils.GetConfig(guild);
            var profile = await utils.Utils.GetProfile(user);

            switch (cfg.defenceLevel)
            {
                case (int)DefenceLevel.soft:
                    if (!profile.localWarns.ContainsKey(guild.Id.ToString()))
                        profile.localWarns[guild.Id.ToString()] = 0;

                    profile.localWarns[guild.Id.ToString()] += 1;

                    if (cfg.warnsLimit > 0 && profile.localWarns[guild.Id.ToString()] >= cfg.warnsLimit)
                    {
                        if (cfg.muteRole == string.Empty)
                            break;
                        var role = guild.GetRole(ulong.Parse(cfg.muteRole));
                        await user.GrantRoleAsync(role).ConfigureAwait(false);
                        res = true;
                    }
                    break;

                case (int)DefenceLevel.hard:
                    if (!profile.localWarns.ContainsKey(guild.Id.ToString()))
                        profile.localWarns[guild.Id.ToString()] = 0;

                    profile.localWarns[guild.Id.ToString()] += 1;

                    if (cfg.warnsLimit > 0 && profile.localWarns[guild.Id.ToString()] >= cfg.warnsLimit)
                    {
                        await user.RemoveAsync("auto-defence level - hard").ConfigureAwait(false);
                        res = true;
                    }
                    break;

                case (int)DefenceLevel.berserker:
                    if (!profile.localWarns.ContainsKey(guild.Id.ToString()))
                        profile.localWarns[guild.Id.ToString()] = 0;

                    profile.localWarns[guild.Id.ToString()] += 1;

                    if (cfg.warnsLimit > 0 && (profile.warns >= cfg.warnsLimit || profile.localWarns[guild.Id.ToString()] >= cfg.warnsLimit))
                    {
                        await user.BanAsync(7, "auto-defence level - berserker").ConfigureAwait(false);
                        res = true;
                    }
                    break;
            }
            return res;
        }

        public static async Task<bool> Punishment(DiscordGuild guild, ulong user)
        {
            var mem = await guild.GetMemberAsync(user);
            return await Punishment(guild, mem);
        }

        public static async Task<bool> CheckSpam(MessageCreateEventArgs args)
        {
            var isSpam = false;
            var cfg = await utils.Utils.GetConfig(args.Guild);
            cfg.antiSpamMode = 1;
            var coll = Bot.mongo.GetDatabase("users").GetCollection<models.User>("profiles");

            var mem = await args.Guild.GetMemberAsync(args.Author.Id);
            var profile = await utils.Utils.GetProfile(mem);

            profile.messagesCount = (int.Parse(profile.messagesCount) + 1).ToString();
            profile.charsCount = (int.Parse(profile.charsCount) + args.Message.Content.Length).ToString();

            var msgs = int.Parse(profile.messagesCount);
            var chars = int.Parse(profile.charsCount);

            switch (cfg.antiSpamMode)
            {
                case (int)utils.AntiSpamMode.messageCount:
                    //1 240
                    if (msgs >= 3 && chars >= 720 && (profile.lastMsg != DateTime.MinValue && (DateTime.UtcNow - profile.lastMsg <= TimeSpan.FromMinutes(1))))
                        profile.spamMsgCount++;

                    if (profile.spamMsgCount >= 3)
                        isSpam = true;
                    break;

                case (int)utils.AntiSpamMode.charsCount:
                    //720 3
                    if (chars >= 240 && msgs >= 1 && (profile.lastMsg != DateTime.MinValue && (DateTime.UtcNow - profile.lastMsg <= TimeSpan.FromMinutes(1))))
                        profile.spamMsgCount++;

                    if (profile.spamMsgCount >= 3)
                        isSpam = true;
                    break;
            }

            profile.lastMsg = DateTime.UtcNow;

            if (isSpam)
            {
                profile.messagesCount = "0";
                profile.charsCount = "0";
                profile.spamMsgCount = 0;
                profile.lastMsg = DateTime.MinValue;
                profile.warns++;
            }

            await coll.ReplaceOneAsync((filter) => filter._id == profile._id, profile).ConfigureAwait(false);
            return isSpam;
        }

        public static async Task<models.User> GetProfile(DiscordUser user)
        {
            var coll = Bot.mongo.GetDatabase("users").GetCollection<models.User>("profiles");
            var cfg = await coll.FindAsync((filter) => filter._id == user.Id.ToString());
            var t = cfg.ToList();
            var res = new models.User(user);

            if (t.Count >= 1)
            {
                foreach (var i in t)
                {
                    res = i;
                }
            }
            else
            {
                await coll.InsertOneAsync(res).ConfigureAwait(false);
            }

            return res;
        }

        public static async Task<List<models.User>> GetProfile(ulong user)
        {
            var coll = Bot.mongo.GetDatabase("users").GetCollection<models.User>("profiles");
            var cfg = await coll.FindAsync((filter) => filter._id == user.ToString());
            var t = cfg.ToList();
            var res = new List<models.User>();

            if (t.Count >= 1)
            {
                foreach (var i in t)
                {
                    res.Add(i);
                }
            }

            return res;
        }

        public static async Task<models.Server> GetConfig(DiscordGuild guild)
        {
            var coll = Bot.mongo.GetDatabase("servers").GetCollection<models.Server>("settings");
            var cfg = await coll.FindAsync((filter) => filter._id == guild.Id.ToString());
            var t = cfg.ToList();
            var res = new models.Server(guild);

            if (t.Count >= 1)
            {
                foreach (var i in t)
                {
                    res = i;
                }
            }
            else
            {
                await coll.InsertOneAsync(res);
            }

            return res;
        }

        public static async Task<List<models.Server>> GetConfig(ulong guild)
        {
            var coll = Bot.mongo.GetDatabase("servers").GetCollection<models.Server>("settings");
            var cfg = await coll.FindAsync((filter) => filter._id == guild.ToString());
            var t = cfg.ToList();
            var res = new List<models.Server>();

            if (t.Count >= 1)
            {
                foreach (var i in t)
                {
                    res.Add(i);
                }
            }

            return res;
        }

        public static async Task<string> GetPrefix(DiscordMessage msg)
        {
            string res = "";
            if (msg.Channel != null && msg.Channel.Guild != null)
            {
                var coll = Bot.mongo.GetDatabase("servers").GetCollection<BsonDocument>("settings");
                var raw = await coll.FindAsync<BsonDocument>(new BsonDocument { { "_id", msg.Channel.Guild.Id.ToString() } });

                foreach (var i in raw.ToList())
                {
                    res = JsonConvert.DeserializeObject<models.Server>(i.ToJson()).prefix;
                }
            }

            return res != "" ? res : Bot.defPrefix;
        }

        public static string TrimNonAscii(string value)
        {
            var l = new List<string>(new Regex(@"[^А-Яа-я -~]+").Replace(value, string.Empty).Split(" "));

            l.RemoveAll(str => string.IsNullOrEmpty(str));

            return string.Join(" ", l);
        }

        public static string TrimNonAscii(string value, string regex = @"[^А-Яа-я -~]+")
        {
            var l = new List<string>(new Regex(regex).Replace(value, string.Empty).Split(" "));

            l.RemoveAll(str => string.IsNullOrEmpty(str));

            return string.Join(" ", l);
        }
    }
}