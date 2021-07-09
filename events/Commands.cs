using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Geno.events
{
    internal class Commands
    {
        public static async Task OnComError(CommandsNextExtension cmd, CommandErrorEventArgs args)
        {
            var isMe = utils.Utils.IsMember(args.Context.Message, args.Context.Guild.CurrentMember);
            var embed = new DiscordEmbedBuilder();

            embed.WithTitle(args.Command.Name);
            embed.WithColor(DiscordColor.Red);
            embed.WithFooter(args.Context.Member.DisplayName, args.Context.Member.AvatarUrl);
            embed.WithTimestamp(DateTime.UtcNow);

            switch (args.Exception.GetType().Name)
            {
                case "ArgumentException":
                    embed.Description = $"Не правильно указаны аргументы, смотрите `help` для большей информации\n";
                    break;

                case "ChecksFailedException":
                    var CFE = (ChecksFailedException)args.Exception;

                    var resUser = new List<string>();
                    var resBot = new List<string>();

                    foreach (var i in CFE.FailedChecks)
                    {
                        if (i.GetType().Name.Contains("RequireUserPermissionsAttribute"))
                        {
                            var err = (RequireUserPermissionsAttribute)i;
                            resUser.Add(err.Permissions.ToPermissionString());
                        }
                        if (i.GetType().Name.Contains("RequireBotPermissionsAttribute"))
                        {
                            var err = (Require​Bot​Permissions​Attribute)i;
                            resBot.Add(err.Permissions.ToPermissionString());
                        }
                        if (i.GetType().Name.Contains("CooldownAttribute"))
                        {
                            var err = (CooldownAttribute)i;
                            embed.Description += utils.Utils.BucketErrorString(err.BucketType,
                                err.GetRemainingCooldown(args.Context).TotalSeconds);
                        }
                    }

                    if (resUser.Count >= 1)
                    {
                        embed.Description += $"Вам не хватает прав {'`' + string.Join("`, `", resUser) + '`'}," +
                            $" для использования команды\n";
                    }
                    if (resBot.Count >= 1)
                    {
                        embed.Description += $"Мне не хватает прав {'`' + string.Join("`, `", resBot) + '`'}," +
                            $" для выполнения команды\n";
                    }
                    break;
            }
            switch (args.Command.Name.ToLower())
            {
                case "ban":
                    var hasPerms = utils.Utils.HasPermissions(args.Context.Guild.CurrentMember, Permissions.BanMembers);

                    switch (args.Exception.GetType().Name)
                    {
                        case "UnauthorizedException":
                            if (isMe)
                            {
                                embed.Description += "Не могу себя забанить\n";
                            }
                            if (utils.Utils.IsMember(args.Context.Message, args.Context.Member))
                            {
                                embed.Description += "Вы не можете себя забанить\n";
                            }
                            if (hasPerms && !isMe)
                            {
                                embed.Description += "Не могу забанить этого пользователя\n";
                            }
                            break;

                        case "ArgumentException":
                            if (isMe)
                            {
                                embed.Description = "Не могу себя забанить\n";
                            }
                            else if (utils.Utils.IsMember(args.Context.Message, args.Context.Member))
                            {
                                embed.Description = "Вы не можете себя забанить\n";
                            }
                            else if (hasPerms && !isMe)
                            {
                                embed.Description = "Не могу забанить этого пользователя\n";
                            }
                            break;
                    }

                    break;
            }

            if (embed.Description == null || embed.Description.Length < 2)
            {
                Console.WriteLine($"\nНе известная ошибка\n{args.Exception}\n" +
                     $"Guild: {args.Context.Guild.Name}({args.Context.Guild.Id})\n" +
                     $"User: {args.Context.Member.Username}#{args.Context.Member.Discriminator}\n" +
                     $"Usage: {args.Context.Message.Content}\n");
            }
            else
            {
                Console.WriteLine($"\n{embed.Description}\n" +
                   $"Guild: {args.Context.Guild.Name}({args.Context.Guild.Id})\n" +
                   $"User: {args.Context.Member.Username}#{args.Context.Member.Discriminator}\n" +
                   $"Usage: {args.Context.Message.Content}\n");

                await args.Context.RespondAsync(embed).ConfigureAwait(false);
            }
            //GC.Collect();
        }

        public static Task CommandHandler(DiscordClient client, MessageCreateEventArgs e)
        {
            Task.Run(async () =>
            {
                if (e.Message.Author.IsBot) return;
                var cnext = client.GetCommandsNext();
                var cmdStart = e.Message.GetStringPrefixLength(e.Guild != null ? await utils.Utils.GetPrefix(e.Message) : Bot.defPrefix);
                if (cmdStart == -1)
                    return;
                var command = cnext.FindCommand(e.Message.Content.ToLower()[cmdStart..], out var args);
                if (command == null)
                    return;

                using (e.Channel.TriggerTypingAsync())
                {
                    await cnext.ExecuteCommandAsync(cnext.CreateContext(e.Message, e.Message.Content.ToLower().Substring(0, cmdStart), command, args));
                }

                try
                {
                    await e.Message.DeleteAsync();
                }
                catch { }
            });

            //GC.Collect();
            return Task.CompletedTask;
        }
    }
}