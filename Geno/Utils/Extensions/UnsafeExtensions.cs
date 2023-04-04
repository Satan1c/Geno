using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Database.Models;
using Discord;
using Geno.Utils.Types;
using ShikimoriSharp.Bases;

namespace Geno.Utils.Extensions;

public static class UnsafeExtensions
{
	public static Overwrite[] GetPermissions(this IEnumerable<Overwrite> permissions, ulong user, ulong firstUser)
	{
		var result = new RefList<Overwrite>(2);
		var list = new RefList<Overwrite>(permissions);
		for (var i = 0; i < list.Count; i++)
		{
			var item = list[i];
			if (item.TargetType != PermissionTarget.User || item.TargetId != user) continue;

			result.Add(item);
		}

		result.Add(new Overwrite(firstUser, PermissionTarget.User,
			new OverwritePermissions(manageChannel: PermValue.Allow)));
		return result.ToArray();
		/*.Where(x => x.TargetType == PermissionTarget.User && x.TargetId != guildUser.Id)
			.Append(new Overwrite(firstUser.Id, PermissionTarget.User, new OverwritePermissions(manageChannel: PermValue.Allow)))
			.ToArray();*/
	}

	public static bool TryGetValue(this IEnumerable<IGuildUser> collection, ulong user, out IGuildUser value)
	{
		var list = new RefList<IGuildUser>(collection);
		for (var i = 0; i < list.Count; i++)
		{
			var item = list[i];
			if (item.IsBot || item.Id == user) continue;

			value = item;
			return true;
		}

		value = default!;
		return false;
	}

	public static T FirstLessEqual<T>(this IEnumerable<T> collection, T find)
		where T : IComparisonOperators<T, T, bool>
	{
		var list = new RefList<T>(collection);
		for (var i = 0; i < list.Count; i++)
		{
			var item = list[i];
			if (item > find)
				return item;
		}

		return default!;
	}

	public static T FirstEqual<T>(this IEnumerable<T> collection, T find)
		where T : IComparisonOperators<T, T, bool>
	{
		var list = new RefList<T>(collection);
		for (var i = 0; i < list.Count; i++)
		{
			var item = list[i];
			if (item == find)
				return item;
		}

		return default!;
	}

	public static AutocompleteResult[] GetAutocompletesUnsafe<T, TT>(this T[] categories,
		ref TT param,
		ref Func<T, TT, (bool, AutocompleteResult)> checker)
	{
		var result = new AutocompleteResult[5];
		ref var startResult = ref MemoryMarshal.GetArrayDataReference(result);
		ref var endResult = ref Unsafe.Add(ref startResult, result.Length);

		ref var start = ref MemoryMarshal.GetArrayDataReference(categories);
		ref var end = ref Unsafe.Add(ref start, categories.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end) && Unsafe.IsAddressLessThan(ref startResult, ref endResult))
		{
			var (check, autocompleteResult) = checker(start, param);
			if (check)
			{
				startResult = autocompleteResult;
				startResult = ref Unsafe.Add(ref startResult, 1);
			}

			start = ref Unsafe.Add(ref start, 1);
		}

		return result;
	}

	public static async Task UpdateRoles(this IGuildUser member, uint adventureRank, GuildDocument doc)
	{
		var role = doc.RankRoles.GetPerfectRoles(adventureRank.ToString());
		var remove = member.RoleIds.GetRemoveRoleIds(ref role, ref doc);

		if (remove.Length > 0)
			await member.RemoveRolesAsync(remove);

		await member.AddRolesAsync(role);
	}

	public static ulong[] GetRemoveRoleIds(this IEnumerable<ulong> roleIds,
		ref ulong[] perfect,
		ref GuildDocument doc)
	{
		var result = new RefList<ulong>();
		var ids = new RefList<ulong>(roleIds);
		var perfectList = new RefList<ulong>(perfect);
		var docList = new RefList<ulong>(doc.RankRoles.Values.SelectMany(x => x));

		for (var i = 0; i < ids.Count; i++)
		{
			var id = ids[i];
			if (docList.Contains(id) && !perfectList.Contains(id))
				result.Add(id);
		}

		return result.ToArray();
	}

	public static AutocompleteResult[] FilterResultUnsafe(this AnimeMangaIdBase?[] tasks, ref UserLocales locale)
	{
		var checker = (AnimeMangaIdBase? result, UserLocales locales) =>
			(result != null, result.AutocompleteResultFrom(locales));
		return tasks.GetAutocompletesUnsafe(ref locale, ref checker);
	}

	public static KeyValuePair<TKey, TValue[]>[] ToArray<TKey, TValue>(
		this RefList<KeyValuePair<TKey, LinkedList<TValue>>> list)
	{
		var arr = new KeyValuePair<TKey, TValue[]>[list.Count];
		ref var start = ref MemoryMarshal.GetArrayDataReference(arr);
		ref var startList = ref MemoryMarshal.GetArrayDataReference(list.ToArray());
		ref var end = ref Unsafe.Add(ref start, arr.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			start = new KeyValuePair<TKey, TValue[]>(startList.Key, startList.Value.ToArray());

			start = ref Unsafe.Add(ref start, 1);
			startList = ref Unsafe.Add(ref startList, 1);
		}

		return arr;
	}

	public static bool ContainsKey<TKey, TValue>(this RefList<KeyValuePair<TKey, TValue>> list, TKey category)
	{
		for (var i = 0; i < list.Count; i++)
			if (AreSame(list[i].Key, category))
				return true;

		return false;
	}

	public static bool TryGetValue<TKey, TValue>(this RefList<KeyValuePair<TKey, TValue>> list,
		TKey category,
		out TValue value)
	{
		for (var i = 0; i < list.Count; i++)
		{
			if (!AreSame(list[i].Key, category)) continue;

			value = list[i].Value;
			return true;
		}

		value = default!;
		return false;
	}

	public static bool ContainsValue<TKey, TValue>(this RefList<KeyValuePair<TKey, TValue>> list, TValue category)
	{
		for (var i = 0; i < list.Count; i++)
			if (AreSame(list[i].Value, category))
				return true;

		return false;
	}

	private static bool AreSame<T>(T left, T right)
	{
		return EqualityComparer<T>.Default.Equals(left, right);
	}
}