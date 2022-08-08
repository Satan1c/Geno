using Discord;
using WargamingApi.WorldOfTanksBlitz.Types.Accounts;

namespace Geno.Utils;

public static class WargamingExtensions
{
    public static EmbedBuilder ApplyRandomStatistics(this EmbedBuilder builder, AccountInfo info)
    {
        var stat = info.Statistics.All;
        return builder.AddField("Random statistics:", "\u200b")
                .AddField("\u200b", "\u200b", true)
                .AddField("\u200b", "\u200b", true)
                .AddField("Battles:", stat.Battles.ToString(), true)
                .AddField("Win rate:", stat.WinRate.ToString(), true)
                .AddField("Avg damage:", (stat.DamageDealt / stat.Battles).ToString(), true)
                .AddField("Avg frags:", (stat.Frags / stat.Battles).ToString(), true)
                .AddField("Frags:", stat.Frags.ToString(), true)
                .AddField("Frags 8+ lvl:", stat.Frags8P.ToString(), true)
            ;
    }

    public static EmbedBuilder ApplyRatingStatistics(this EmbedBuilder builder, AccountInfo info)
    {
        var stat = info.Statistics.Rating;
        return builder.AddField("Rating statistics:", "\u200b")
                .AddField("\u200b", "\u200b", true)
                .AddField("\u200b", "\u200b", true)
                .AddField("Battles:", stat.Battles.ToString(), true)
                .AddField("Win rate:", stat.WinRate.ToString(), true)
                .AddField("Avg damage:", (stat.DamageDealt / stat.Battles).ToString(), true)
                .AddField("Avg frags:", (stat.Frags / stat.Battles).ToString(), true)
                .AddField("Frags:", stat.Frags.ToString(), true)
                .AddField("Frags 8+ lvl:", stat.Frags8P.ToString(), true)
            ;
    }

    public static EmbedBuilder ApplyClanStatistics(this EmbedBuilder builder, AccountInfo info)
    {
        var stat = info.Statistics.Clan;
        return builder.AddField("Clan statistics:", "\u200b")
                .AddField("\u200b", "\u200b", true)
                .AddField("\u200b", "\u200b", true)
                .AddField("Battles:", stat.Battles.ToString(), true)
                .AddField("Win rate:", stat.WinRate.ToString(), true)
                .AddField("Avg damage:", (stat.DamageDealt / stat.Battles).ToString(), true)
                .AddField("Avg frags:", (stat.Frags / stat.Battles).ToString(), true)
                .AddField("Frags:", stat.Frags.ToString(), true)
                .AddField("Frags 8+ lvl:", stat.Frags8P.ToString(), true)
            ;
    }
}