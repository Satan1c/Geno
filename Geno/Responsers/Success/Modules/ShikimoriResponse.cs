using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Localization;
using Localization.Models;
using ShikimoriSharp.Classes;

namespace Geno.Responsers.Success.Modules;

public static class ShikimoriResponse
{
	private static Category s_category;

	private const string c_baseUrl = "https://shikimori.me/";
	private static readonly Regex s_characterRegex =
		new(@"\[(?:character=\w+|\/character)\]", RegexOptions.Compiled | RegexOptions.Singleline);

	public static void Init(LocalizationManager localizationManager)
	{
		s_category = localizationManager.GetCategory("genshin");
	}

	public static ValueTask SearchResult(this ShardedInteractionContext context, MangaID? manga = null)
	{
		return manga == null
			? context.Interaction.Respond(new EmbedBuilder().WithTitle("Nothing found"), ephemeral: true, isDefered: true)
			: context.Interaction.Respond(manga.GetMangaEmbed(), ephemeral: false, isDefered: true);
	}

	public static ValueTask SearchResult(this ShardedInteractionContext context, AnimeID? anime = null)
	{
		return anime == null
			? context.Interaction.Respond(new EmbedBuilder().WithTitle("Nothing found"), ephemeral: true, isDefered: true)
			: context.Interaction.Respond(anime.GetAnimeEmbed(), ephemeral: false, isDefered: true);
	}

	private static EmbedBuilder GetAnimeEmbed(this AnimeID anime)
	{
		var url = string.Concat(c_baseUrl, anime.Url);
		var descriptionBuilder = new StringBuilder()
			.AppendFormat("Status: `{0}`\n", anime.Status);

		if (anime is { Episodes: > 0, EpisodesAired: > 0 })
			descriptionBuilder.AppendFormat(
				"Episodes: `{0}`\n", string.Concat(
					anime.EpisodesAired.ToString(),
					anime.Status != "released" ? string.Concat("` / `", anime.Episodes.ToString()) : ""));

		if (anime.Genres != null)
			descriptionBuilder.AppendFormat(
				"\n `Genres`: `{0}`", string.Join("`, `", anime.Genres.Select(x => x.Name)));

		if (anime.Studios != null)
			descriptionBuilder.AppendFormat(
				"\n `Studios`: `{0}`", string.Join("`, `", anime.Studios.Select(x => x.Name)));

		if (anime.Score != null)
			descriptionBuilder.AppendFormat(
				"\n `Score`: `{0}`", anime.Score);

		if (anime.Franchise != null)
			descriptionBuilder.AppendFormat(
				"\n[Franchise]({0}/franchise)\n", url);

		descriptionBuilder
			.Append('\n')
			.Append(anime.Description ?? anime.DescriptionSource ?? "");

		return new EmbedBuilder()
			.WithTitle(string.Concat(
				string.IsNullOrEmpty(anime.Russian)
					? ""
					: string.Concat(anime.Russian, " / "), anime.Name))
			.WithUrl(url)
			.WithImageUrl(string.Concat(c_baseUrl, anime.Image?.Original))
			.WithDescription(descriptionBuilder.ClearDescription());
	}

	private static EmbedBuilder GetMangaEmbed(this MangaID manga)
	{
		var url = string.Concat(c_baseUrl, manga.Url);
		var descriptionBuilder =
			new StringBuilder();

		if (manga.Franchise != null)
			descriptionBuilder.AppendFormat(
				"\n[Franchise]({0}/franchise)\n", url);

		if (manga is { Volumes: > 0, Chapters: > 0 })
			descriptionBuilder.AppendFormat(
				"Volumes: `{0}`\nChapters: `{1}`\n", manga.Volumes.ToString(), manga.Chapters.ToString());

		if (manga.Genres != null)
			descriptionBuilder.AppendFormat(
				"\n `Genres`: `{0}`", string.Join("`, `", manga.Genres.Select(x => x.Name)));

		if (manga.Publishers != null)
			descriptionBuilder.AppendFormat(
				"\n `Publishers`: `{0}`", string.Join("`, `", manga.Publishers.Select(x => x.Name)));

		if (manga.Score != null)
			descriptionBuilder.AppendFormat(
				"\n `Score`: `{0}`", manga.Score);

		descriptionBuilder
			.Append('\n')
			.Append(manga.English?.FirstOrDefault() ?? manga.Description ?? manga.DescriptionSource ?? "");

		return new EmbedBuilder()
			.WithTitle(string.Concat(
				string.IsNullOrEmpty(manga.Russian)
					? ""
					: string.Concat(manga.Russian, " / "), manga.Name))
			.WithUrl(url)
			.WithImageUrl(string.Concat(c_baseUrl, manga.Image?.Original))
			.WithDescription(descriptionBuilder.ClearDescription());
	}

	private static string ClearDescription(this StringBuilder descriptionBuilder)
	{
		return descriptionBuilder.ToString().ClearDescription();
	}

	private static string ClearDescription(this string description)
	{
		return s_characterRegex.Replace(description, "");
	}
}