﻿using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Geno.Utils.Types;
using Newtonsoft.Json;

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

	/*private static readonly Regex s_noAsciiRegex = new(
		"[^А-Яа-я -~]+",
		RegexOptions.Compiled | RegexOptions.Singleline
	);*/

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
		return JsonConvert.DeserializeObject<UserLocales>(JsonConvert.SerializeObject(context.Interaction.UserLocale));
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

	public static ulong[] GetPerfectRole(this Dictionary<string, ulong[]> roles, string rank)
	{
		if (roles.ContainsKey(rank))
			return roles[rank];

		var keys = roles.Keys;
		var first = keys.First();
		var rankByte = byte.Parse(rank);

		return
			rankByte >= byte.Parse(first)
				? roles[first]
				: roles[keys.Select(byte.Parse).FirstOrDefault(i => rankByte >= i).ToString()];
	}

	internal static bool HasLink(this SocketMessage message)
	{
		return s_linkRegex.IsMatch(message.Content);
	}

	public static string UserTag(this IUser user)
	{
		return user.Username + '#' + user.Discriminator;
	}

	internal static bool TryGetRole(this SocketGuild guild, ulong id, out SocketRole role)
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

	internal static bool TryGetInvite(this BaseSocketClient client, string code, out RestInviteMetadata invite)
	{
		invite = null!;

		try
		{
			if (client.GetInviteAsync(code).Result is not { } res)
				return false;

			invite = res;
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryGetGuild(this DiscordRestClient client, ulong id, out RestGuild guild)
	{
		guild = null!;

		try
		{
			if (client.GetGuildAsync(id).Result is not { } res)
				return false;

			guild = res;
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryGetUser(this DiscordRestClient client, ulong id, out RestUser user)
	{
		user = null!;

		try
		{
			if (client.GetUserAsync(id).Result is not { } res)
				return false;

			user = res;
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryGetGuildUser(this DiscordRestClient client, ulong guildId, ulong userId,
		out RestGuildUser user)
	{
		user = null!;

		try
		{
			if (client.GetGuildUserAsync(guildId, userId).Result is not { } res)
				return false;

			user = res;
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static async ValueTask<bool> HasInvite(this IDiscordClient client, SocketUserMessage message,
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
}