using Discord;
using Discord.Rest;

namespace Geno.Utils;

public static class EmbedExtensions
{
    public static EmbedBuilder ApplyData(this EmbedBuilder builder, RestInviteMetadata invite)
    {
        builder.Description ??= "";
        builder.Description += invite.GuildId;

        builder.Author ??= new EmbedAuthorBuilder();
        builder.Author.Name = invite.GuildName;
        builder.Author.Url = $"https://discord.gg/{invite.Code}";
        
        builder.AddField("Inviter", invite.Inviter.ToString())
            .AddField("Is Temporary", invite.IsTemporary.ToString(), true);

        if (invite.IsTemporary)
            builder.AddField("Expire At", $"{invite.MaxAge.ToString()}: <t:{invite.MaxAge.ToString()}:R>", true);

        return builder;
    }

    public static EmbedBuilder ApplyData(this EmbedBuilder builder, RestUser user)
    {
        builder.WithTitle("User info");
        builder.WithDescription($"Tag: `{user.UserTag()}`");

        builder.WithThumbnailUrl(user.GetAvatarUrl(ImageFormat.Auto, 2048));
        builder.WithImageUrl(user.GetBannerUrl(ImageFormat.Auto, 2048));

        builder.AddField("Id:", $"`{user.Id.ToString()}`");
        builder.AddField("Is bot:", $"`{user.IsBot.ToString()}`");
        builder.AddField("Badges:", $"`{string.Join("`, `", user.PublicFlags.ToString()!.Split(", "))}`");
        builder.AddField("Created at:", $"<t:{user.CreatedAt.ToUnixTimeSeconds().ToString()}:R>");
        return builder;
    }

    public static EmbedBuilder ApplyData(this EmbedBuilder builder, RestGuildUser user)
    {
        builder.Title = "Member info";
        builder.Fields.RemoveAt(builder.Fields.Count - 1);

        if (builder.Description.Split('#')[0] != user.DisplayName)
            builder.Description = $"Nickname: `{user.DisplayName}`\n{builder.Description}";


        builder.AddField("Created at:", $"<t:{user.CreatedAt.ToUnixTimeSeconds().ToString()}:R>", true);
        builder.AddField("Joined at:", $"<t:{user.JoinedAt!.Value.ToUnixTimeSeconds().ToString()}:R>", true);
        return builder;
    }

    public static EmbedBuilder ApplyData(this EmbedBuilder builder, RestGuild guild)
    {
        builder.Description ??= "";
        builder.Description += guild.Description;
        
        builder.Author ??= new EmbedAuthorBuilder();
        builder.Author.Name = guild.Name;
        builder.Author.IconUrl = guild.IconUrl;
        
        builder.AddField("Created at", $"<t:{guild.CreatedAt.ToUnixTimeSeconds().ToString()}:R>");

        if (!string.IsNullOrEmpty(guild.SplashUrl) && !string.IsNullOrWhiteSpace(guild.SplashUrl))
            builder.WithThumbnailUrl(guild.SplashUrl);

        return builder;
    }
}