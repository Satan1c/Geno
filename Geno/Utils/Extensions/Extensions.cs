﻿using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI.DataBinder;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Geno.Utils.Types;
using ShikimoriSharp.Bases;

namespace Geno.Utils.Extensions;

public static class Extensions
{
	private static readonly Regex s_inviteRegex = new(
		"discord(?:\\.com|app\\.com|\\.gg)[\\/invite\\/]?(?:[a-zA-Z0-9\\-]{2,32})",
		RegexOptions.Compiled | RegexOptions.Singleline
	);

	private static readonly Regex s_linkRegex = new(
		"(http|ftp|https):\\/\\/([\\w_-]+(?:(?:\\.[\\w_-]+)+))([\\w.,@?^=%&:\\/~+#-]*[\\w@?^=%&\\/~+#-])",
		RegexOptions.Compiled | RegexOptions.Singleline
	);

	private static readonly Regex s_formatRegex =
		new(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+",
			RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

	public static readonly Regex CodeRegex =
		new(@"([A-Z0-9]{10,12})", RegexOptions.Compiled | RegexOptions.Singleline);

	/*private static readonly Regex s_noAsciiRegex = new(
		"[^А-Яа-я -~]+",
		RegexOptions.Compiled | RegexOptions.Singleline
	);*/

	public static AutocompleteResult AutocompleteResultFrom(this AnimeMangaIdBase? animeManga, UserLocales locales)
	{
		if (animeManga == null)
			return new AutocompleteResult();

		return new AutocompleteResult(
			locales == UserLocales.Russian
				? string.IsNullOrEmpty(animeManga.Russian)
					? animeManga.English.FirstOrDefault() ?? animeManga.Name
					: animeManga.Russian
				: animeManga.English.FirstOrDefault() ?? animeManga.Name,
			animeManga.Name);
	}

	public static bool HasFlags(this Optional<MessageFlags?> target, MessageFlags categories)
	{
		return ((MessageFlags)target!)!.HasFlags(categories);
	}

	public static bool HasFlags(this MessageFlags target, MessageFlags categories)
	{
		return ((byte)target & (byte)categories) != 0;
	}

	public static bool HasCategory(this Category target, Category categories)
	{
		return ((byte)target & (byte)categories) != 0;
	}

	public static UserLocales GetLocale(this IInteractionContext context)
	{
		return context.Interaction.UserLocale == "ru" ? UserLocales.Russian : UserLocales.English;
		//JsonConvert.DeserializeObject<UserLocales>(JsonConvert.SerializeObject(context.Interaction.UserLocale));
	}

	public static ModuleInfo GetTopLevelModule(this ModuleInfo module)
	{
		do
		{
			if (module.Parent is { } info)
				module = info;
		} while (!module.IsTopLevelGroup);

		return module;
	}

	public static ulong[] GetPerfectRoles(this IDictionary<string, ulong[]> roles, string rank)
	{
		if (roles.TryGetValue(rank, out var value))
			return value;

		var keys = roles.Keys;
		var first = keys.First();
		var rankByte = byte.Parse(rank);

		return
			rankByte >= byte.Parse(first)
				? roles[first]
				: roles[keys.Select(s => byte.Parse(s)).FirstLessEqual(rankByte).ToString()];
	}

	public static bool HasLink(this SocketMessage message)
	{
		return s_linkRegex.IsMatch(message.Content);
	}

	public static string UserTag(this IUser user)
	{
		return user.Username + '#' + user.Discriminator;
	}

	public static bool TryGetRole(this SocketGuild guild, ulong id, out SocketRole role)
	{
		try
		{
			role = guild.GetRole(id);
			return true;
		}
		catch
		{
			role = null!;
			return false;
		}
	}

	public static bool TryGetInvite(this BaseSocketClient client, string code, out RestInviteMetadata invite)
	{
		invite = null!;

		try
		{
			if (client.GetInviteAsync(code).GetAwaiter().GetResult() is not { } res)
				return false;

			invite = res;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryGetGuild(this DiscordRestClient client, ulong id, out RestGuild guild)
	{
		guild = null!;

		try
		{
			if (client.GetGuildAsync(id).GetAwaiter().GetResult() is not { } res)
				return false;

			guild = res;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryGetUser(this DiscordRestClient client, ulong id, out RestUser user)
	{
		user = null!;

		try
		{
			if (client.GetUserAsync(id).GetAwaiter().GetResult() is not { } res)
				return false;

			user = res;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryGetGuildUser(this DiscordRestClient client, ulong guildId, ulong userId, out IGuildUser user)
	{
		user = null!;

		try
		{
			if (client.GetGuildUserAsync(guildId, userId).GetAwaiter().GetResult() is not { } res)
				return false;

			user = res;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static async ValueTask<bool> HasInvite(this IDiscordClient client,
		SocketUserMessage message,
		bool fetchForValidation = false,
		bool ignoreCurrentServer = false)
	{
		var match = s_inviteRegex.Match(message.Content);

		if (!match.Success)
			return false;

		if (!fetchForValidation)
			return true;

		var inviteId = match.Value.Split("/")[^1];
		var invite = await client.GetInviteAsync(inviteId);

		if (invite.Code == null)
			return false;

		return !ignoreCurrentServer || invite.GuildId != (message.Author as SocketGuildUser)?.Guild.Id;
	}

	public static string FormatWith<T>(this string format, T source)
	{
		return format.FormatWith(source, null);
	}

	private static string FormatWith(this string format, object source, IFormatProvider? provider)
	{
		if (format == null)
			throw new ArgumentNullException(nameof(format));

		var values = new LinkedList<object>();
		var rewrittenFormat = s_formatRegex.Replace(format, m =>
		{
			var leftBracket = m.Groups["start"];
			var propertyName = m.Groups["property"];
			var formatGroup = m.Groups["format"];
			var rightBracket = m.Groups["end"];

			try
			{
				values.AddLast(propertyName.Value == "0"
					? source
					: DataBinder.Eval(source, propertyName.Value));
			}
			catch
			{
				return m.ToString();
			}

			var index = (values.Count - 1).ToString();
			return new StringBuilder()
				.Append(leftBracket.Captures.Count > 0 ? '{' : new char())
				.Append(index)
				.Append(rightBracket.Captures.Count > 0 ? '}' : new char())
				.ToString();
		});

		try
		{
			var res = values.Count == 0
				? format
				: string.Format(provider, rewrittenFormat, values.ToArray());

			return res;
		}
		catch
		{
			// ignored
		}

		return rewrittenFormat;
	}
}