using Discord;
using Discord.Interactions;
using Geno.Types;
using Geno.Utils;
using Microsoft.Extensions.DependencyInjection;
using WargamingApi.Types.Enums;
using WargamingApi.WorldOfTanksBlitz;
using WargamingApi.WorldOfTanksBlitz.Services;

namespace Geno.Commands;

[Group("wg", "wargaming commands group")]
public class Wargaming : InteractionModuleBase<ShardedInteractionContext>
{
    [Group("blitz", "WoT Blitz commands group")]
    public class Blitz : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly Accounts m_accounts;

        public Blitz(IServiceProvider provider)
        {
            var blitzClient = provider.GetRequiredService<WorldOfTanksBlitzClient>().Services;
            m_accounts = blitzClient.GetRequiredService<Accounts>();
        }

        [SlashCommand("search_accounts", "search blitz account by nickname")]
        public async Task SearchAccount(Regions region, string nickname, byte? limit = 9)
        {
            var resp = await m_accounts.SearchAccounts(region, nickname, limit: limit);
            if (resp.Error is not null) throw new ArgumentException(resp.Error.Value.Message);

            var data = resp.Data;
            var embed = new EmbedBuilder()
                .WithTitle("Account list");

            foreach (var i in data) embed.AddField(i.Nickname, $"`{i.AccountId.ToString()}`", true);

            await RespondAsync(embed: embed.Build(),
                allowedMentions: AllowedMentions.None);
        }

        [SlashCommand("account_info", "get account info by id")]
        public async Task<RuntimeResult> GetAccountInfo(Regions region, string accountId)
        {
            var accountIds = accountId.Split(' ').Select(x =>
            {
                if (long.TryParse(x, out var id))
                    return id;

                throw new ArgumentException($"Provide valid value for {nameof(accountId)}");
                //return new InteractionResult(InteractionCommandError.BadArgs, $"Provide valid value for {nameof(accountId)}");
            });

            var resp = await m_accounts.GetAccountInfo(region, accountIds);
            var data = resp.Data;
            var embeds = new Embed[data.Count];

            foreach (var (k, v) in data)
            {
                var embed = new EmbedBuilder();

                if (v is not null)
                    embed.WithTitle(v.Nickname)
                        .WithDescription($"`{k.ToString()}`")
                        .ApplyRandomStatistics(v)
                        .ApplyRatingStatistics(v)
                        .ApplyClanStatistics(v);
                else
                    embed.WithTitle(k.ToString())
                        .WithDescription("No info was found");

                embeds[embeds.Length] = embed.Build();
            }

            await RespondAsync(embeds: embeds);

            return new InteractionResult(null, "");
        }
    }
}