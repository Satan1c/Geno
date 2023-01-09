using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Geno.Utils;

public static class EmbedExtensions
{
	public const string Empty = "\u200b";

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

	public static EmbedBuilder ApplyData(this EmbedBuilder builder, SocketGuild guild)
	{
		builder.Description ??= "";
		builder.Description += guild.Description;

		builder.Author ??= new EmbedAuthorBuilder();
		builder.Author.Name = guild.Name;
		builder.Author.IconUrl = guild.IconUrl;

		builder.AddField("Created at", $"<t:{guild.CreatedAt.ToUnixTimeSeconds().ToString()}:R>")
			.AddField("Flags:", $"`{string.Join("`, `", guild.Features.Value.ToString().Split(", "))}`")
			.AddField("Member count:", $"`{guild.MemberCount.ToString()}`/`{guild.MaxMembers.ToString()}`");

		if (!string.IsNullOrEmpty(guild.SplashUrl) && !string.IsNullOrWhiteSpace(guild.SplashUrl))
			builder.WithThumbnailUrl(guild.SplashUrl);


		return builder;
	}

	public static EmbedBuilder ApplyData(this EmbedBuilder builder, RestUser user)
	{
		return builder.WithTitle("User info")
			.WithDescription($"Tag: `{user.UserTag()}`")
			.WithThumbnailUrl(user.GetAvatarUrl(ImageFormat.Auto, 2048))
			.WithImageUrl(user.GetBannerUrl(ImageFormat.Auto, 2048))
			.AddField("Id:", $"`{user.Id.ToString()}`")
			.AddField("Is bot:", $"`{user.IsBot.ToString()}`")
			.AddField("Badges:", $"`{string.Join("`, `", user.PublicFlags.ToString()!.Split(", "))}`")
			.AddField("Created at:", $"<t:{user.CreatedAt.ToUnixTimeSeconds().ToString()}:R>");
		;
	}

	public static EmbedBuilder AddEmpty(this EmbedBuilder builder, byte count, bool isInline = true)
	{
		for (byte i = 0; i < count; i++)
			builder.AddField(Empty, Empty, isInline);

		return builder;
	}

	public static EmbedBuilder ApplyData(this EmbedBuilder builder, RestGuildUser user)
	{
		builder.Title = "Member info";
		builder.Fields.RemoveAt(builder.Fields.Count - 1);

		if (builder.Description.Split('#')[0] != user.DisplayName)
			builder.Description = $"Nickname: `{user.DisplayName}`\n{builder.Description}";

		return builder.AddField("Created at:", $"<t:{user.CreatedAt.ToUnixTimeSeconds().ToString()}:R>", true)
			.AddField("Joined at:", $"<t:{user.JoinedAt!.Value.ToUnixTimeSeconds().ToString()}:R>", true);
		;
	}
}