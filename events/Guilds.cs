using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Geno.events
{
    internal class Guilds
    {
        public static async Task OnMessage(DiscordClient client, MessageCreateEventArgs args)
        {
            if (args.Message.Author.IsBot) return;
            var cfg = await utils.Utils.GetConfig(args.Guild);
            if (cfg.antiSpamMode == (int)utils.AntiSpamMode.disabled && !cfg.antiInvite) return;

            var mem = await args.Guild.GetMemberAsync(args.Author.Id);
            var hasPerms = utils.Utils.HasPermissions(await args.Guild.GetMemberAsync(args.Author.Id),
                Permissions.BanMembers |
                    Permissions.KickMembers |
                    Permissions.ManageGuild |
                    Permissions.ManageChannels |
                    Permissions.ManageMessages |
                    Permissions.MuteMembers |
                    Permissions.DeafenMembers, true,
                client);
            if (hasPerms) return;

            var profile = await utils.Utils.GetProfile(mem);
            var coll = Bot.mongo.GetDatabase("users").GetCollection<models.User>("profiles");
            if (int.Parse(profile.messagesCount) >= 5)
            {
                profile.messagesCount = "0";
                profile.charsCount = "0";
                profile.spamMsgCount = 0;
                profile.lastMsg = DateTime.MinValue;
                await coll.ReplaceOneAsync((filter) => filter._id == profile._id, profile).ConfigureAwait(false);
            }

            if (cfg.antiInvite)
            {
                var clear = utils.Utils.TrimNonAscii(args.Message.Content).Split(" ");
                foreach (var i in clear)
                {
                    var focused = i.Split("/");
                    if (focused.Length <= 1)
                        continue;

                    var raw = await Bot.HTTPClient.GetAsync($"https://discord.com/api/v8/invites/{focused[^1]}");
                    var res = await raw.Content.ReadAsStringAsync();
                    var invite = JsonConvert.DeserializeObject<DiscordInvite>(res);

                    if (invite.Code != focused[^1])
                        continue;

                    profile.warns++;
                    await coll.ReplaceOneAsync((filter) => filter._id == profile._id, profile).ConfigureAwait(false);

                    var embed = new DiscordEmbedBuilder();
                    embed.WithColor(DiscordColor.Orange);
                    embed.WithTitle("Пиар");
                    embed.AddField("Сервер", $"{args.Guild.Name}\n{args.Guild.Id}\n{args.Guild.MemberCount}");
                    embed.AddField("Нарушитель", $"{args.Author.Username}#{args.Author.Discriminator}\n{args.Author.Id}");
                    embed.AddField("Сообщение", args.Message.Content);
                    embed.WithThumbnail(args.Guild.IconUrl);
                    embed.WithTimestamp(DateTime.UtcNow);

                    await Bot.reportChannel.SendMessageAsync(embed).ConfigureAwait(false);

                    await utils.Utils.Punishment(args.Guild, args.Author);
                    break;
                }
            }

            if (cfg.antiSpamMode != (int)utils.AntiSpamMode.disabled)
            {
                var spam = await utils.Utils.CheckSpam(args);
                if (spam)
                {
                    var embed = new DiscordEmbedBuilder();
                    embed.WithColor(DiscordColor.Orange);
                    embed.WithTitle("Спам");
                    embed.AddField("Сервер", $"{args.Guild.Name}\n{args.Guild.Id}\n{args.Guild.MemberCount}");
                    embed.AddField("Нарушитель", $"{args.Author.Username}#{args.Author.Discriminator}\n{args.Author.Id}");
                    embed.AddField("Сообщение", args.Message.Content);
                    embed.WithThumbnail(args.Guild.IconUrl);
                    embed.WithTimestamp(DateTime.UtcNow);

                    await Bot.reportChannel.SendMessageAsync(embed).ConfigureAwait(false);

                    await utils.Utils.Punishment(args.Guild, args.Author);
                }
            }

            //GC.Collect();
        }
    }
}