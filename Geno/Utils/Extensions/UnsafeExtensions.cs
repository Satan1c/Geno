using System.Diagnostics.Metrics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Database.Models;
using Discord;
using Geno.Utils.Types;
using ShikimoriSharp.Bases;
using WaifuPicsApi.Enums;

namespace Geno.Utils.Extensions;

public static class UnsafeExtensions
{
	public static AutocompleteResult[] FilterResultUnsafe(this AutocompleteResult[] categories, ref string userInput)
	{
		var checker = (AutocompleteResult result, string input) =>
			(result.Name.StartsWith(input, StringComparison.InvariantCultureIgnoreCase), result);

		return categories.GetAutocompletesUnsafe(ref userInput, ref checker);
	}

	public static void GenerateCategoriesUnsafe(ref AutocompleteResult[] sfwCategories)
	{
		var categoriesList = new RefList<AutocompleteResult>(sfwCategories.Length);
		ref var start = ref MemoryMarshal.GetReference(sfwCategories.AsSpan());
		ref var end = ref Unsafe.Add(ref start, sfwCategories.Length);

		var counter = 0;
		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			var category = (SfwCategory)counter;
			var name = category.EnumToString();
			
			categoriesList.Add(new AutocompleteResult(name, name));
			counter++;
			
			start = ref Unsafe.Add(ref start, 1);
		}

		sfwCategories = categoriesList.ToArray();
	}
	
	public static Overwrite[] GetPermissions(this IEnumerable<Overwrite> permissions, ulong user, ulong firstUser)
	{
		var result = new RefList<Overwrite>(2);
		var list = new RefList<Overwrite>(permissions).AsSpan();
		ref var start = ref MemoryMarshal.GetReference(list);
		ref var end = ref Unsafe.Add(ref start, list.Length);
		
		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			if (start.TargetType == PermissionTarget.User && start.TargetId != user)
				result.Add(start);
			
			start = ref Unsafe.Add(ref start, 1);
		}

		result.Add(new Overwrite(firstUser, PermissionTarget.User,
			new OverwritePermissions(manageChannel: PermValue.Allow)));
		return result.ToArray();
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
		var remove = member.RoleIds.ToArray().GetRemoveRoleIds(ref role, ref doc);

		if (remove.Length > 0)
			await member.RemoveRolesAsync(remove);

		await member.AddRolesAsync(role);
	}

	public static ulong[] GetRemoveRoleIds(this ulong[] roleIds,
		ref ulong[] perfect,
		ref GuildDocument doc)
	{
		var result = new RefList<ulong>(roleIds.Length);
		var ids = roleIds.AsSpan();
		var perfectSpan = perfect.AsSpan();
		var docList = doc.RankRoles.Values.SelectMany(x => x).ToArray().AsSpan();

		ref var id = ref MemoryMarshal.GetReference(ids);
		ref var end = ref Unsafe.Add(ref id, ids.Length);
		
		while (Unsafe.IsAddressLessThan(ref id, ref end))
		{
			if (docList.Contains(id) && !perfectSpan.Contains(id))
				result.Add(id);

			id = ref Unsafe.Add(ref id, 1);
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
		var listArray = list.ToArray().AsSpan();
		var arr = new KeyValuePair<TKey, TValue[]>[listArray.Length];
		var arrSpan = arr.AsSpan();
		
		ref var start = ref MemoryMarshal.GetReference(arrSpan);
		ref var listRef = ref MemoryMarshal.GetReference(listArray);
		ref var end = ref Unsafe.Add(ref start, arr.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			start = new KeyValuePair<TKey, TValue[]>(listRef.Key, listRef.Value.ToArray());
			
			start = ref Unsafe.Add(ref start, 1);
			listRef = ref Unsafe.Add(ref listRef, 1);
		}

		return arr;
	}

	public static bool TryGetValue<TKey, TValue>(this RefList<KeyValuePair<TKey, TValue>> list,
		TKey category,
		out TValue value)
	{
		ref var start = ref MemoryMarshal.GetReference(list.AsSpan());
		ref var end = ref Unsafe.Add(ref start, list.Count);
		
		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			if (!AreSame(start.Key, category))
			{
				start = ref Unsafe.Add(ref start, 1);
				continue;
			}

			value = start.Value;
			return true;
		}

		value = default!;
		return false;
	}

	private static bool AreSame<T>(T left, T right)
	{
		return EqualityComparer<T>.Default.Equals(left, right);
	}
}