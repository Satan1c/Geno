using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Geno.commands
{
    public class Other : BaseCommandModule
    {
        public Other()
        {
            var names = new string[]
            {
                "help",
                "bot",
                "status",
                "warns",
                "test"
            };

            foreach (var i in names)
            {
                Bot.help[i] = "Other";
            }
        }

        [Command("test"),
            RequireOwner,
            Hidden]
        public async Task Test(CommandContext ctx, [RemainingText] string txt)
        {
            Console.WriteLine(DateTime.UtcNow);
            await Task.Delay(10 * 1000);
            Console.WriteLine(DateTime.UtcNow);
        }

        [Command("warns"),
            Aliases(new string[] { "w" }),
            Description("Выводит кол-во варнов, за спам\n" +
            ":-:\n" +
            "Пример использования: `{0}warns`\n`{0}warns` `{1}`"),
            Cooldown(1, Bot.smallCD, CooldownBucketType.User)]
        public async Task Warns(CommandContext ctx)
        {
            await Warns(ctx, ctx.Member.Id);
        }

        [Command("warns")]
        public async Task Warns(CommandContext ctx, DiscordMember user)
        {
            await Warns(ctx, user.Id);
        }

        [Command("warns")]
        public async Task Warns(CommandContext ctx, ulong user = 0)
        {
            var raw = await utils.Utils.GetProfile(user);
            if (raw.Count <= 0) throw new ArgumentException("no profile");
            var profile = raw[0];
            var mem = await ctx.Client.GetUserAsync(user);
            var embed = new DiscordEmbedBuilder();

            embed.WithAuthor($"{mem.Username}#{mem.Discriminator}", iconUrl: mem.AvatarUrl);
            embed.WithDescription($"Wars: {profile.warns}");
            embed.WithColor(profile.warns == 0 ? DiscordColor.Green : profile.warns >= 1 ? DiscordColor.Orange : DiscordColor.Red);

            await ctx.RespondAsync(embed).ConfigureAwait(false);
        }

        [Command("help"),
            Aliases(new string[] { "h" }),
            Hidden,
            Cooldown(1, Bot.smallCD, CooldownBucketType.Guild)]
        public async Task Help(CommandContext ctx, string cmdName = "")
        {
            var cmdnext = ctx.Client.GetCommandsNext();
            var formatter = new CustomHelp(ctx);
            if (cmdName != "")
            {
                if (cmdnext.RegisteredCommands.ContainsKey(cmdName.ToLower()))
                {
                    formatter.WithCommand(cmdnext.FindCommand(cmdName.ToLower(), out _));
                    await ctx.RespondAsync(formatter.Build().Embed).ConfigureAwait(false);
                    return;
                }
                else
                {
                    formatter.WithCategory(cmdName.ToLower());
                }
            }

            var pages = await formatter.Start();
            await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);//.ConfigureAwait(false);
        }

        [Command("bot"),
            Aliases(new string[] { "info" }),
            Description("Выводит некоторую информацию о боте, например сколько у него серверов\n" +
            ":-:\n" +
            "Пример использования: `{0}bot`"),
            Cooldown(1, Bot.smallCD, CooldownBucketType.User)]
        public async Task Ping(CommandContext ctx)
        {
            var frame = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
            var embed = new DiscordEmbedBuilder();

            var owner = await ctx.Client.GetUserAsync(348444859360608256);

            embed.Title = ctx.Client.CurrentUser.Username;
            embed.Color = DiscordColor.Green;
            embed.WithAuthor(owner.Username + "#" + owner.Discriminator, "https://discord.com/invite/NSkg6N9", owner.AvatarUrl);

            #region with System.Management
            using (ManagementObjectSearcher cpu = new ManagementObjectSearcher("select * from Win32_Processor"), os = new ManagementObjectSearcher("select * from Win32_OperatingSystem"))
            {
                foreach (var i in os.Get())
                {
                    embed.AddField("OC:", $"`{i["Caption"]}`", true);
                }
                foreach (var i in cpu.Get())
                {
                    var freq = i["CurrentClockSpeed"].ToString();
                    embed.AddField("ЦП:", $"Ядра: `{i["NumberOfLogicalProcessors"]}` \n" +
                        $"Частота: `{freq[0] + "." + freq[2]}`ghz \n" +
                        $"Использование: `{i["LoadPercentage"]}`%", true);
                }
            }
            #endregion
            #region For Linux without System.Management
            /*
            embed.AddField("OC:", $"`{string.Join(" ", Environment.OSVersion.VersionString.Split(" ")[..2])}`", true);
            embed.AddField("ЦП:", $"Ядра: `{Environment.ProcessorCount}` \n" +
                        $"Частота: `NoData`ghz \n" +
                        $"Использование: `NoData`%", true);*/
            #endregion
            embed.AddField("ОЗУ:", $"`{Math.Round((decimal)(Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024)), 2)}`мб", true);

            embed.AddField("Сервера:", $"`{ctx.Client.Guilds.Count}`", true);
            embed.AddField("Пинг:", $"`{ctx.Client.Ping}`мс", true);
            embed.AddField("\u200b", "\u200b", true);

            embed.AddField($"Версия {frame.Split(",")[0]}:", $"`{frame.Split(",")[1].Split("=")[1]}`", true);
            embed.AddField("Версия DSharpPlus:", $"`{ctx.Client.VersionString}`", true);
            embed.AddField("\u200b", "\u200b", true);

            embed.WithFooter(ctx.Member.DisplayName, ctx.Member.AvatarUrl);
            embed.WithTimestamp(DateTime.Now);

            await ctx.RespondAsync(embed).ConfigureAwait(false);
        }

        [Command("status"),
            Aliases(new string[] { "setup" }),
            Description("Проверяет совместимость прав и функций бота\n" +
            ":-:\n" +
            "Пример использования: `{0}status`"),
            Cooldown(1, Bot.smallCD, CooldownBucketType.Guild)]
        public async Task Status(CommandContext ctx)
        {
            var me = ctx.Guild.CurrentMember;
            var embed = new DiscordEmbedBuilder();
            embed.WithAuthor(me.DisplayName, iconUrl: ctx.Client.CurrentUser.AvatarUrl);
            embed.WithThumbnail(ctx.Guild.IconUrl);
            embed.WithFooter(ctx.Member.DisplayName, ctx.Member.AvatarUrl);
            embed.WithTimestamp(DateTime.UtcNow);

            var work = 0f;
            var all = 7f;

            if (utils.Utils.HasPermissions(me, Permissions.BanMembers))
            {
                embed.AddField("Права бана участников", "✅");
                work++;
            }
            else
            {
                embed.AddField("Права бана участников", "❎");
            }

            if (utils.Utils.HasPermissions(me, Permissions.ViewAuditLog))
            {
                embed.AddField("Права просмотра журнала аудита", "✅");
                work++;
            }
            else
            {
                embed.AddField("Права просмотра журнала аудита", "❎");
            }

            if (utils.Utils.HasPermissions(me, Permissions.ManageNicknames))
            {
                embed.AddField("Права управления никами", "✅");
                work++;
            }
            else
            {
                embed.AddField("Права управления никами", "❎");
            }

            if (utils.Utils.HasPermissions(me, Permissions.MuteMembers | Permissions.DeafenMembers))
            {
                embed.AddField("Права заглушить/откл микрофон пользователей", "✅");
                work++;
            }
            else
            {
                embed.AddField("Права заглушить/откл микрофон пользователей", "❎");
            }

            if (utils.Utils.HasPermissions(me, Permissions.AddReactions | Permissions.UseExternalEmojis))
            {
                embed.AddField("Права добавлять реакции и использовать внешние емодзи", "✅");
                work++;
            }
            else
            {
                embed.AddField("Права добавлять реакции и использовать внешние емодзи", "❎");
            }

            if (utils.Utils.HasPermissions(me, Permissions.ReadMessageHistory))
            {
                embed.AddField("Права читать историю сообщений", "✅");
                work++;
            }
            else
            {
                embed.AddField("Права читать историю сообщений", "❎");
            }

            if (utils.Utils.HasPermissions(me, Permissions.ManageMessages))
            {
                embed.AddField("Права управления сообщениями", "✅");
                work++;
            }
            else
            {
                embed.AddField("Права управления сообщениями", "❎");
            }

            int percent = (int)MathF.Round((work / all) * 100, 0);
            embed.WithDescription($"Работоспособность бота: {percent}%");
            embed.WithColor(percent >= 70 ? DiscordColor.Green : percent >= 40 ? DiscordColor.Orange : DiscordColor.Red);

            await ctx.RespondAsync(embed: embed).ConfigureAwait(false);
        }
    }
}