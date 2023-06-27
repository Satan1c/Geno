using System.Text;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Geno.Utils.Extensions;

public static class EmbedExtensions
{
	public const string Empty = "\u200b";

	public static (EmbedBuilder builder, ComponentBuilder components) GetRegistrationForm(ulong id)
	{
		var builder = new EmbedBuilder()
			.WithDescription(
				$"<@{id.ToString()}> doesn't link HoYoLab account\n How to link them: [guide](https://geno.satan1c.com/guides?id=LinkHoYoLab)");
		var components = new ComponentBuilder()
			.AddRow(new ActionRowBuilder()
				.WithButton(new ButtonBuilder()
					.WithLabel("Register").WithStyle(ButtonStyle.Primary)
					.WithCustomId("hoyo_registration_button")));
		return (builder, components);
	}

	public static EmbedBuilder ApplyData(this EmbedBuilder builder, RestInviteMetadata invite, bool extra = false)
	{
		var guild = invite.PartialGuild;
		var id = invite.GuildId?.ToString() ?? "";
		var description = string.IsNullOrEmpty(builder.Description?.Trim() ?? string.Empty)
			? id
			: builder.Description + '\n' + id;

		builder
			.WithAuthor(invite.GuildName, guild.IconUrl, invite.Url)
			.WithDescription(description)
			.AddField("Inviter", invite.Inviter.ToString())
			.AddField("Uses", invite.Uses.ToString(), true)
			.AddField("Max Uses", invite.MaxUses.ToString(), true)
			.AddField("Is Temporary", invite.IsTemporary.ToString(), true);

		if (invite.CreatedAt != null)
			builder.AddField("Invite created At", $"<t:{invite.CreatedAt?.ToUnixTimeSeconds().ToString()}:R>", true);
		if (invite.IsTemporary)
			builder.AddField("Expire At", $"{invite.MaxAge.ToString()}: <t:{invite.MaxAge.ToString()}:R>", true);
		if (invite is { CreatedAt: not null, IsTemporary: true })
			builder.AddEmpty();

		if (!extra) return builder;

		builder
			.AddField("Member count:", $"`{invite.MemberCount.ToString()}`", true)
			.AddField("Flags:",
				$"`{string.Join("`, `", guild.Features.Value.GuildFeaturesToString().Split(", "))}`");

		if (string.IsNullOrEmpty(guild.BannerUrl?.Trim() ?? string.Empty))
			builder.WithImageUrl(guild.BannerUrl);

		if (string.IsNullOrEmpty(guild.SplashUrl?.Trim() ?? string.Empty))
			builder.WithThumbnailUrl(guild.SplashUrl);

		return builder;
	}

	public static EmbedBuilder ApplyData(this EmbedBuilder builder, SocketGuild guild)
	{
		var description = string.IsNullOrEmpty(builder.Description?.Trim() ?? string.Empty)
			? guild.Description
			: builder.Description + '\n' + guild.Description;

		builder
			.WithDescription(description)
			.WithAuthor(guild.Name, guild.IconUrl)
			.AddField("Guild created at", $"<t:{guild.CreatedAt.ToUnixTimeSeconds().ToString()}:R>")
			.AddField("Flags:", $"`{string.Join("`, `", guild.Features.Value.GuildFeaturesToString().Split(", "))}`")
			.AddField("Member count:", $"`{guild.MemberCount.ToString()}`/`{guild.MaxMembers.ToString()}`");

		if (!string.IsNullOrEmpty(guild.SplashUrl?.Trim() ?? string.Empty))
			builder.WithThumbnailUrl(guild.SplashUrl);

		return builder;
	}

	public static EmbedBuilder ApplyData(this EmbedBuilder builder, RestUser user)
	{
		var flags = string.Join("`, `", user.PublicFlags.PublicFlagsToString().Split(", "));
		return builder.WithTitle("User info")
			.WithDescription(
				$"Tag: `{user.Username}{(string.IsNullOrEmpty(user.Discriminator) || user.Discriminator == "0000" ? "" : user.Discriminator)}`")
			.WithThumbnailUrl(user.GetAvatarUrl(ImageFormat.Auto, 2048))
			.WithImageUrl(user.GetBannerUrl(ImageFormat.Auto, 2048))
			.AddField("Id:", $"`{user.Id.ToString()}`")
			.AddField("Is bot:", $"`{user.IsBot.ToString()}`")
			.AddField("Badges:", $"`{(string.IsNullOrEmpty(flags) ? "None" : flags)}`")
			.AddField("Created at:", $"<t:{user.CreatedAt.ToUnixTimeSeconds().ToString()}:R>");
	}

	public static EmbedBuilder AddEmpty(this EmbedBuilder builder, byte count, bool isInline = true)
	{
		for (byte i = 0; i < count; i++)
			builder.AddEmpty(isInline);

		return builder;
	}

	public static EmbedBuilder AddEmpty(this EmbedBuilder builder, bool isInline = true)
	{
		builder.AddField(Empty, Empty, isInline);

		return builder;
	}

	public static EmbedBuilder ApplyData(this EmbedBuilder builder, IGuildUser user)
	{
		builder.Title = "Member info";
		builder.Fields.RemoveAt(builder.Fields.Count - 1);

		if (builder.Description.Split('#')[0] != user.DisplayName)
			builder.Description = $"Nickname: `{user.DisplayName}`\n{builder.Description}";

		return builder.AddField("Created at:", $"<t:{user.CreatedAt.ToUnixTimeSeconds().ToString()}:R>", true)
			.AddField("Joined at:", $"<t:{user.JoinedAt!.Value.ToUnixTimeSeconds().ToString()}:R>", true);
	}

	public static string GuildFeaturesToString(this GuildFeature features)
	{
		var result = new StringBuilder();

		if (features.HasFeature(GuildFeature.AnimatedBanner))
			result.Add(nameof(GuildFeature.AnimatedBanner));

		if (features.HasFeature(GuildFeature.AnimatedIcon))
			result.Add(nameof(GuildFeature.AnimatedIcon));

		if (features.HasFeature(GuildFeature.Banner))
			result.Add(nameof(GuildFeature.Banner));

		if (features.HasFeature(GuildFeature.ChannelBanner))
			result.Add(nameof(GuildFeature.ChannelBanner));

		if (features.HasFeature(GuildFeature.Commerce))
			result.Add(nameof(GuildFeature.Commerce));

		if (features.HasFeature(GuildFeature.Community))
			result.Add(nameof(GuildFeature.Community));

		if (features.HasFeature(GuildFeature.Discoverable))
			result.Add(nameof(GuildFeature.Discoverable));

		if (features.HasFeature(GuildFeature.DiscoverableDisabled))
			result.Add(nameof(GuildFeature.DiscoverableDisabled));

		if (features.HasFeature(GuildFeature.EnabledDiscoverableBefore))
			result.Add(nameof(GuildFeature.EnabledDiscoverableBefore));

		if (features.HasFeature(GuildFeature.Featureable))
			result.Add(nameof(GuildFeature.Featureable));

		if (features.HasFeature(GuildFeature.ForceRelay))
			result.Add(nameof(GuildFeature.ForceRelay));

		if (features.HasFeature(GuildFeature.HasDirectoryEntry))
			result.Add(nameof(GuildFeature.HasDirectoryEntry));

		if (features.HasFeature(GuildFeature.Hub))
			result.Add(nameof(GuildFeature.Hub));

		if (features.HasFeature(GuildFeature.InternalEmployeeOnly))
			result.Add(nameof(GuildFeature.InternalEmployeeOnly));

		if (features.HasFeature(GuildFeature.InviteSplash))
			result.Add(nameof(GuildFeature.InviteSplash));

		if (features.HasFeature(GuildFeature.LinkedToHub))
			result.Add(nameof(GuildFeature.LinkedToHub));

		if (features.HasFeature(GuildFeature.MemberProfiles))
			result.Add(nameof(GuildFeature.MemberProfiles));

		if (features.HasFeature(GuildFeature.MemberVerificationGateEnabled))
			result.Add(nameof(GuildFeature.MemberVerificationGateEnabled));

		if (features.HasFeature(GuildFeature.MoreEmoji))
			result.Add(nameof(GuildFeature.MoreEmoji));

		if (features.HasFeature(GuildFeature.News))
			result.Add(nameof(GuildFeature.News));

		if (features.HasFeature(GuildFeature.NewThreadPermissions))
			result.Add(nameof(GuildFeature.NewThreadPermissions));

		if (features.HasFeature(GuildFeature.Partnered))
			result.Add(nameof(GuildFeature.Partnered));

		if (features.HasFeature(GuildFeature.PremiumTier3Override))
			result.Add(nameof(GuildFeature.PremiumTier3Override));

		if (features.HasFeature(GuildFeature.PreviewEnabled))
			result.Add(nameof(GuildFeature.PreviewEnabled));

		if (features.HasFeature(GuildFeature.PrivateThreads))
			result.Add(nameof(GuildFeature.PrivateThreads));

		if (features.HasFeature(GuildFeature.RelayEnabled))
			result.Add(nameof(GuildFeature.RelayEnabled));

		if (features.HasFeature(GuildFeature.RoleIcons))
			result.Add(nameof(GuildFeature.RoleIcons));

		if (features.HasFeature(GuildFeature.RoleSubscriptionsAvailableForPurchase))
			result.Add(nameof(GuildFeature.RoleSubscriptionsAvailableForPurchase));

		if (features.HasFeature(GuildFeature.RoleSubscriptionsEnabled))
			result.Add(nameof(GuildFeature.RoleSubscriptionsEnabled));

		if (features.HasFeature(GuildFeature.SevenDayThreadArchive))
			result.Add(nameof(GuildFeature.SevenDayThreadArchive));

		if (features.HasFeature(GuildFeature.TextInVoiceEnabled))
			result.Add(nameof(GuildFeature.TextInVoiceEnabled));

		if (features.HasFeature(GuildFeature.ThreadsEnabled))
			result.Add(nameof(GuildFeature.ThreadsEnabled));

		if (features.HasFeature(GuildFeature.ThreadsEnabledTesting))
			result.Add(nameof(GuildFeature.ThreadsEnabledTesting));

		if (features.HasFeature(GuildFeature.ThreadsDefaultAutoArchiveDuration))
			result.Add(nameof(GuildFeature.ThreadsDefaultAutoArchiveDuration));

		if (features.HasFeature(GuildFeature.ThreeDayThreadArchive))
			result.Add(nameof(GuildFeature.ThreeDayThreadArchive));

		if (features.HasFeature(GuildFeature.TicketedEventsEnabled))
			result.Add(nameof(GuildFeature.TicketedEventsEnabled));

		if (features.HasFeature(GuildFeature.VanityUrl))
			result.Add(nameof(GuildFeature.VanityUrl));

		if (features.HasFeature(GuildFeature.Verified))
			result.Add(nameof(GuildFeature.Verified));

		if (features.HasFeature(GuildFeature.VIPRegions))
			result.Add(nameof(GuildFeature.VIPRegions));

		if (features.HasFeature(GuildFeature.WelcomeScreenEnabled))
			result.Add(nameof(GuildFeature.WelcomeScreenEnabled));

		if (features.HasFeature(GuildFeature.DeveloperSupportServer))
			result.Add(nameof(GuildFeature.DeveloperSupportServer));

		if (features.HasFeature(GuildFeature.InvitesDisabled))
			result.Add(nameof(GuildFeature.InvitesDisabled));

		if (features.HasFeature(GuildFeature.AutoModeration))
			result.Add(nameof(GuildFeature.AutoModeration));

		return result.ToString();
	}

	public static string PublicFlagsToString(this UserProperties? flags, string separator = ", ")
	{
		if (flags == null)
			return nameof(UserProperties.None);

		var result = new StringBuilder();

		if (flags.HasProperty(UserProperties.System))
		{
			result.Add(nameof(UserProperties.System));
			return result.ToString();
		}

		if (flags.HasProperty(UserProperties.BotHTTPInteractions))
		{
			result.Add(nameof(UserProperties.EarlyVerifiedBotDeveloper));
			return result.ToString();
		}

		if (flags.HasProperty(UserProperties.Staff))
			result.Add(nameof(UserProperties.Staff));

		if (flags.HasProperty(UserProperties.Partner))
			result.Add(nameof(UserProperties.Partner));

		if (flags.HasProperty(UserProperties.DiscordCertifiedModerator))
			result.Add(nameof(UserProperties.DiscordCertifiedModerator));

		if (flags.HasProperty(UserProperties.HypeSquadEvents))
			result.Add(nameof(UserProperties.HypeSquadEvents));

		if (flags.HasProperty(UserProperties.HypeSquadBravery))
			result.Add(nameof(UserProperties.HypeSquadBravery));
		else if (flags.HasProperty(UserProperties.HypeSquadBalance))
			result.Add(nameof(UserProperties.HypeSquadBalance));
		else if (flags.HasProperty(UserProperties.HypeSquadBrilliance))
			result.Add(nameof(UserProperties.HypeSquadBrilliance));

		if (flags.HasProperty(UserProperties.BugHunterLevel1))
			result.Add(nameof(UserProperties.BugHunterLevel1));
		else if (flags.HasProperty(UserProperties.BugHunterLevel2))
			result.Add(nameof(UserProperties.BugHunterLevel2));

		if (flags.HasProperty(UserProperties.VerifiedBot))
			result.Add(nameof(UserProperties.VerifiedBot));
		if (flags.HasProperty(UserProperties.ActiveDeveloper))
			result.Add(nameof(UserProperties.ActiveDeveloper));
		if (flags.HasProperty(UserProperties.EarlyVerifiedBotDeveloper))
			result.Add(nameof(UserProperties.EarlyVerifiedBotDeveloper));

		if (flags.HasProperty(UserProperties.EarlySupporter))
			result.Add(nameof(UserProperties.EarlySupporter));

		if (flags.HasProperty(UserProperties.TeamUser))
			result.Add(nameof(UserProperties.TeamUser));

		return result.ToString();
	}

	public static StringBuilder Add(this StringBuilder builder, string value, string separator = ", ")
	{
		return builder.Length == 0
			? builder.Append(value)
			: builder.Append(separator).Append(value);
	}

	public static bool HasProperty(this UserProperties? flags, UserProperties flag)
	{
		return ((uint)(flags ?? UserProperties.None) & (uint)flag) != 0;
	}

	public static bool HasFeature(this GuildFeature flags, GuildFeature flag)
	{
		return ((ulong)flags & (ulong)flag) != 0;
	}
}