using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Geno.commands
{
    public class CustomHelp : BaseHelpFormatter
    {
        protected DiscordEmbedBuilder _embed;
        protected Dictionary<string, List<Command>> Categories = new Dictionary<string, List<Command>>();

        public CustomHelp(CommandContext ctx) : base(ctx)
        {
            _embed = new DiscordEmbedBuilder();
            _embed.WithTitle("Help");
            _embed.WithDescription("`<>` - обязательный аргумет\n" +
                "`[]` - опциональный аргумент");
            _embed.WithColor(DiscordColor.Green);

            foreach (var i in Context.Client.GetCommandsNext().RegisteredCommands)
            {
                var key = Bot.help[i.Value.Name.ToLower()];
                if (!Categories.ContainsKey(key))
                    Categories[key] = new List<Command>();

                Categories[key].Add(i.Value);
            }
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            var prefix = utils.Utils.GetPrefix(Context.Message).GetAwaiter().GetResult();

            if (!command.IsHidden)
            {
                var desc = command.Description.Split(":-:");
                desc[0] += $"Варианты использования: `{command.Name}`, `{string.Join("`, `", command.Aliases)}`";
                desc[1] = string.Format(desc[1], prefix, Context.Member.Id, Context.Member.DisplayName, Context.Member.Mention);
                _embed.AddField(command.Name, string.Join("\n", desc));
            }

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> cmds)
        {
            foreach (var cmd in cmds)
            {
                _embed.AddField(cmd.Name, cmd.Description);
            }

            return this;
        }

        public BaseHelpFormatter WithCategory(string category)
        {
            var cmds = Categories[category];
            this.WithSubcommands(cmds);

            return this;
        }

        public override CommandHelpMessage Build()
        {
            return new CommandHelpMessage(embed: _embed);
        }

        public async Task<List<Page>> Start()
        {
            var pages = new List<Page>();
            var index = 1;
            var isOwner = Context.Client.CurrentApplication.Owners.Any(x => x.Id == Context.Member.Id);

            foreach (var cat in Categories)
            {
                var embed = new DiscordEmbedBuilder();
                embed.Color = _embed.Color;
                embed.Description = string.Empty;
                embed.Title = cat.Key;
                embed.WithFooter($"Страница [{index}/{Categories.Count}]");

                var passed = new List<string>();
                var unpassed = new List<string>();

                foreach (var i in cat.Value)
                {

                    if (!passed.Contains(i.Name) && (!i.IsHidden || isOwner))
                    {
                        var checks = true;
                        foreach (var c in await i.RunChecksAsync(Context, false))
                        {
                            if (c.GetType().Name == "RequireUserPermissionsAttribute" || c.GetType().Name == "RequireBotPermissionsAttribute")
                            {
                                checks = false;
                                unpassed.Add(i.Name);
                                break;
                            }
                        }

                        if (checks)
                        {
                            if (i.Description != null)
                            {
                                var desc = i.Description.Split(":-:");
                                embed.AddField(i.Name, desc[0]);
                            }
                        }
                    }

                    passed.Add(i.Name);
                }

                if (unpassed.Count >= 1)
                {
                    embed.WithColor(DiscordColor.Red);
                    embed.Title += "\nНекоторые команды не доступны, из-за не хватки прав у вас/бота";
                    embed.Description += $"\n```css\n[{string.Join(" ,", unpassed)}]\n```";
                }

                embed.Description += _embed.Description;
                pages.Add(new Page(embed: embed));
                index++;
            }

            return pages;
        }
    }
}