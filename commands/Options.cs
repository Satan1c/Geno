using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Geno.commands
{
    internal class Options : BaseCommandModule
    {
        public Options()
        {
            var names = new string[]
            {
                "settings"
            };

            foreach (var i in names)
            {
                Bot.help[i] = "System";
            }
        }

        [Command("settings"),
            Aliases(new string[] { "options" }),
            utils.Utils.RequireUserPermissions(Permissions.ManageGuild),
            Description("Изменяет настройки сервера, в системе бота, параметры должны быть указаны через пробел после их значения а также и меть `-` в начале\n" +
            "При указании нескольких параметров сразу, разделяйте их `;` после названия параметра\n" +
            "Требует права для выполнения:\n" +
            "- пользователь - ManageGuild\n" +
            ":-:\n" +
            "Использование: `{0}settings` `<параметры>`\n" +
            "Для списка параметров, введите `{0}settings` `help`\n" +
            "Для списка текущих значений параметров, введите `{0}settings` `current`\n" +
            "Пример использования: `{0}settings` `new_prefix -prefix`\n" +
            "`{0}settings` `new_prefix -prefix;` `true -clearNicknames`"),
            Cooldown(1, Bot.middleCD, CooldownBucketType.Guild)]
        public async Task Settings(CommandContext ctx, [RemainingText] string args)
        {
            if (args.Length <= 1)
                throw new ArgumentException();

            var coll = Bot.mongo.GetDatabase("servers").GetCollection<models.Server>("settings");
            var cfg = await utils.Utils.GetConfig(ctx.Guild);

            if (args.ToLower() == "help")
            {
                await SettingsHelp(ctx, cfg).ConfigureAwait(false);
                return;
            }
            else if (args.ToLower() == "current")
            {
                await SettingsCurrent(ctx, cfg).ConfigureAwait(false);
                return;
            }

            var parsed = args.Split("; ");

            if (parsed.Length <= 1)
            {
                Execute(args, cfg, ctx);
            }
            else
            {
                foreach (var i in parsed)
                {
                    Execute(i, cfg, ctx);
                }
            }
            await coll.ReplaceOneAsync((filter) => filter._id == cfg._id, cfg);
        }

        private void Execute(string args, models.Server cfg, CommandContext ctx)
        {
            var list = new List<string>()
            {
                "prefix",
                "prf",

                "muterole",
                "mr",

                "defencelevel",
                "dl",

                "warnslimit",
                "wl",

                "clearnicknames",
                "cn",

                "allowmanualnicknamechange",
                "amnc",

                "antispamMode",
                "asm",

                "antiinvite",
                "ai",
            };
            var parsed = args.Split(" -");

            if (parsed.Length < 1 || !list.Contains(parsed[1]))
                throw new ArgumentException();

            var cleared = utils.Utils.TrimNonAscii(parsed[0]);
            
            switch (parsed[1])
            {
                case "prefix":
                case "prf":
                    if (cleared.Length > 10 || cleared.Length < 1)
                        throw new ArgumentException();
                    cfg.prefix = cleared;
                    break;

                case "muterole":
                case "mr":
                    var mute = ulong.TryParse(cleared, out var RoleID);
                    if (mute && ctx.Guild.Roles.Any((x) => x.Key == RoleID))
                    {
                        cfg.muteRole = RoleID.ToString();
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                        
                    break;

                case "defencelevel":
                case "dl":
                    switch (cleared)
                    {
                        case "soft":
                            cfg.antiSpamMode = (int)utils.DefenceLevel.soft;
                            break;

                        case "harg":
                            cfg.antiSpamMode = (int)utils.DefenceLevel.hard;
                            break;

                        case "charsCount":
                            cfg.antiSpamMode = (int)utils.DefenceLevel.berserker;
                            break;
                    }
                    break;

                case "warnslimit":
                case "wl":
                    if (!int.TryParse(cleared, out var wl) || wl > byte.MaxValue || wl < 0)
                        throw new ArgumentException();

                    cfg.warnsLimit = byte.Parse(wl.ToString());
                    break;

                case "clearnicknames":
                case "cn":
                    if (!bool.TryParse(cleared, out var cn))
                        throw new ArgumentException();
                    /*
                    if (cn)
                        await utils.Utils.Rename(await ctx.Guild.GetAllMembersAsync());*/
                    cfg.clearNicknames = cn;
                    break;

                case "allowmanualnicknamechange":
                case "amnc":
                    if (!bool.TryParse(cleared, out var amnc))
                        throw new ArgumentException();
                    cfg.allowManualNicknameChange = amnc;
                    break;

                case "antispamode":
                case "asm":
                    switch (cleared)
                    {
                        case "disabled":
                            cfg.antiSpamMode = (int)utils.AntiSpamMode.disabled;
                            break;

                        case "messageCount":
                            cfg.antiSpamMode = (int)utils.AntiSpamMode.messageCount;
                            break;

                        case "charsCount":
                            cfg.antiSpamMode = (int)utils.AntiSpamMode.charsCount;
                            break;
                    }
                    break;

                case "antiinvite":
                case "ai":
                    if (!bool.TryParse(cleared, out var ai))
                        throw new ArgumentException();
                    cfg.antiInvite = ai;
                    break;
            }
        }

        private async Task SettingsHelp(CommandContext ctx, models.Server cfg)
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithColor(DiscordColor.Green);
            embed.WithTitle("Список параметров settings");
            embed.WithFooter(ctx.Member.DisplayName, ctx.Member.AvatarUrl);
            embed.WithTimestamp(DateTime.UtcNow);

            var defServer = new models.Server(ctx.Guild);
            var ASM = defServer.antiSpamMode == 0 ? utils.AntiSpamMode.disabled : defServer.antiSpamMode == 1 ? utils.AntiSpamMode.messageCount : utils.AntiSpamMode.charsCount;
            var DL = defServer.defenceLevel == 0 ? utils.DefenceLevel.soft : defServer.defenceLevel == 1 ? utils.DefenceLevel.hard : utils.DefenceLevel.berserker;
            DiscordRole top = null;
            foreach (var i in ctx.Member.Roles)
            {
                top = i;
                break;
            }

            embed.AddField($"prefix",
                "Меняет префикс, для сервера, длина префикса должна быть в диапазоне 1-10\n" +
                "Ильзование:\n`new_prefix -PRF`\nдля сброса изменений: `defPrefix -PRF`\n" +
                $"Стандартно:\n`{defServer.prefix}`\n" +
                "Варианты использования:\n `prefix`, `PRF`", true);

            embed.AddField("muteRole",
                "Устанавливает роль, для мьюта нарушителей, указывается по айди\n" +
                $"Использование:\n`{top.Id} -MR`\n" +
                $"Стандартно:\n`{(defServer.muteRole != string.Empty ? defServer.muteRole : "нету")}`\n" +
                "Варианты использования:\n `muteRole`, `MR`", true);

            embed.AddField("antiInvite",
                "Переключает наказание за рекламу(*инвайт на сервер*)\n" +
                $"Ильзование:\n `{(!cfg.antiInvite).ToString().ToLower()} -AI`\n" +
                $"Стандартно:\n `{defServer.antiInvite.ToString().ToLower()}`\n" +
                "Варианты использования:\n `antiInvite`, `AI`", true);

            embed.AddField("warnsLimit",
                $"Устанавливает лимит нарушений, для сервера, в пределах 0-{byte.MaxValue}, для отключения наказания за нарушения - укажите `0`\n" +
                "Использование:\n`1 -WL`\n" +
                $"Стандартно:\n`{defServer.warnsLimit}`\n" +
                $"Варианты использования:\n `warnsLimit`, `WL`", true);

            embed.AddField("clearNicknames",
                "Переключает автоматическую очистку никнеймов, новых и текущих пользователей\n" +
                $"Ильзование:\n`{(!cfg.clearNicknames).ToString().ToLower()} -CN`\n" +
                $"Стандартно:\n`{defServer.clearNicknames.ToString().ToLower()}`\n" +
                "Варианты использованя:\n `clearNicknames`, `CN`", true);

            embed.AddField("allowManualNicknameChange",
                "Переключает возможность менять никнеймы вручную, при включенном `clearNicknames`\n" +
                $"Ильзование:\n`{(!cfg.allowManualNicknameChange).ToString().ToLower()} -AMNC`\n" +
                $"Стандартно:\n`{defServer.allowManualNicknameChange.ToString().ToLower()}`\n" +
                "Варианты использованя:\n `allowManualNicknameChange`, `AMNC`", true);

            embed.AddField("antiSpamMode",
                "Переключает режим для регистрации спамма\n" +
                "Ильзование:\n`messageCount -ASM`\n" +
                "Режимы:\n" +
                "- **disabled** - отключает\n" +
                "- **messagesCount** - режим определения, по кол-ву сообщений\n" +
                "- **charsCount** - режим определения, по кол-ву символов в сообщениях\n" +
                $"Стандартно:\n`{ASM}`\n" +
                "Варианты использованя:\n `antiSpamMode`, `ASM`", true);

            embed.AddField("defenceLevel",
                "Переключает уровень защиты\n" +
                "Использование:\n`hard -DL`\n" +
                "Уровни:\n" +
                "- **soft** - игнорирует глобальные варны нарушителя, выдает мьют, если была указана роль и число нарушений на сервере превысило установленый лимит\n" +
                "- **hard** - игнорирует глобальные варны нарушителя, кикает с сервера, если число нарушений на сервере превысило установленый лимит\n" +
                "- **berserker** - банит нарушителя, если количество глобальных варнов или нарушений на сервере первысило установленый лимит\n" +
                $"Стандартно:\n`{DL}`\n" +
                $"Варианты использования:\n `defenceLevel`, `DL`", true);

            await ctx.RespondAsync(embed).ConfigureAwait(false);
        }

        public async Task SettingsCurrent(CommandContext ctx, models.Server cfg)
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithTitle("Список текущих параметров settings");
            embed.WithColor(DiscordColor.Green);
            embed.WithFooter(ctx.Member.DisplayName, ctx.Member.AvatarUrl);
            embed.WithTimestamp(DateTime.UtcNow);

            embed.AddField("prefix", cfg.prefix);
            embed.AddField("clearNicknames", cfg.clearNicknames.ToString().ToLower());
            embed.AddField("allowManualNicknameChange", cfg.allowManualNicknameChange.ToString().ToLower());
            embed.AddField("antiSpamMode", cfg.antiSpamMode == 0 ? "disabled" : cfg.antiSpamMode == 1 ? "messagesCount" : "charsCount");
            embed.AddField("defenceLevel", cfg.defenceLevel == 0 ? "soft" : cfg.defenceLevel == 1 ? "hard" : "berserker");
            embed.AddField("antiInvite", cfg.antiInvite.ToString().ToLower());
            embed.AddField("warnsLimit", cfg.warnsLimit.ToString().ToLower());

            bool mute = false;
            try
            {
                mute = ctx.Guild.Roles.Any((x) => x.Key.ToString() == cfg.muteRole);
            }
            catch
            {
                mute = false;
            }
            embed.AddField("muteRole", mute ? $"<@&{cfg.muteRole}>" : cfg.muteRole != string.Empty && cfg.muteRole.Length > 1 ? "Указана не действительная роль" : "Роль не задана");

            await ctx.RespondAsync(embed).ConfigureAwait(false);
        }
    }
}