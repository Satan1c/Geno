using Discord;
using Discord.Interactions;
using Geno.Utils.Services;
using Geno.Utils.Types;
using WaifuPicsApi;
using WaifuPicsApi.Enums;

namespace Geno.Commands.Private;

[Group("img", "images group")]
[Private(Category.Images)]
public class Images : InteractionModuleBase<ShardedInteractionContext>
{
	private readonly WaifuClient m_waifuClient = new();
	//private IEnumerable<AutocompleteResult> m_sfwCategories = Array.Empty<AutocompleteResult>();

	[SlashCommand("nsfw", "nsfw images")]
	[EnabledInDm(false)]
	[RequireNsfw]
	public async Task NsfwCommands(NsfwCategory tag)
	{
		var img = await m_waifuClient.GetImageAsync(tag);
		Console.WriteLine(img);

		await RespondAsync(embed: new EmbedBuilder()
				.WithImageUrl(img)
				.Build(),
			allowedMentions: AllowedMentions.None);
	}

	[SlashCommand("sfw", "sfw images")]
	[EnabledInDm(false)]
	public async Task SfwCommands([Autocomplete(typeof(SfwAutocompleteHandler))] string tag, IUser? user = null)
	{
		try
		{
			if (!Enum.TryParse<SfwCategory>(tag, out var category)) return;

			var ctf = GetCategoryFormat(category);
			if (ctf == CategoryFormat.User && user == null)
			{
				await RespondAsync($"You must provide {nameof(user)} for this category");
				return;
			}

			var embed = new EmbedBuilder();
			var title = ctf switch
			{
				CategoryFormat.Neutral => tag,
				CategoryFormat.Solo => string.Format(GetStringForCategory(category),
					$"<@{Context.User.Id.ToString()}>"),
				CategoryFormat.User => string.Format(GetStringForCategory(category), $"<@{Context.User.Id.ToString()}>",
					$"<@{(user == null ? throw new ArgumentNullException(nameof(user), "user must be provided") : user.Id.ToString())}>"),
				_ => throw new ArgumentOutOfRangeException()
			};

			var img = await m_waifuClient.GetImageAsync(category);

			await RespondAsync(embed: embed
					.WithDescription(title)
					.WithImageUrl(img)
					.Build(),
				allowedMentions: AllowedMentions.None);
		}
		catch (Exception e)
		{
			await ClientEvents.OnLog(
				new LogMessage(
					LogSeverity.Error,
					$"{nameof(Images)} {nameof(SfwCommands)}",
					e.Message,
					e));
		}
	}


	private static CategoryFormat GetCategoryFormat(SfwCategory category)
	{
		return category switch
		{
			SfwCategory.Waifu => CategoryFormat.Neutral,
			SfwCategory.Neko => CategoryFormat.Neutral,
			SfwCategory.Shinobu => CategoryFormat.Neutral,
			SfwCategory.Megumin => CategoryFormat.Neutral,
			SfwCategory.Bully => CategoryFormat.User,
			SfwCategory.Cuddle => CategoryFormat.User,
			SfwCategory.Cry => CategoryFormat.Solo,
			SfwCategory.Hug => CategoryFormat.User,
			SfwCategory.Awoo => CategoryFormat.Neutral,
			SfwCategory.Kiss => CategoryFormat.User,
			SfwCategory.Lick => CategoryFormat.User,
			SfwCategory.Pat => CategoryFormat.User,
			SfwCategory.Smug => CategoryFormat.Solo,
			SfwCategory.Bonk => CategoryFormat.User,
			SfwCategory.Yeet => CategoryFormat.User,
			SfwCategory.Blush => CategoryFormat.Solo,
			SfwCategory.Smile => CategoryFormat.Solo,
			SfwCategory.Wave => CategoryFormat.Solo,
			SfwCategory.Highfive => CategoryFormat.User,
			SfwCategory.Handhold => CategoryFormat.User,
			SfwCategory.Nom => CategoryFormat.Solo,
			SfwCategory.Bite => CategoryFormat.User,
			SfwCategory.Glomp => CategoryFormat.User,
			SfwCategory.Slap => CategoryFormat.User,
			SfwCategory.Kill => CategoryFormat.User,
			SfwCategory.Kick => CategoryFormat.User,
			SfwCategory.Happy => CategoryFormat.Solo,
			SfwCategory.Wink => CategoryFormat.Solo,
			SfwCategory.Poke => CategoryFormat.User,
			SfwCategory.Dance => CategoryFormat.Solo,
			SfwCategory.Cringe => CategoryFormat.Solo,
			_ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
		};
	}

	private static string GetStringForCategory(SfwCategory category)
	{
		return category switch
		{
			SfwCategory.Bully => "{0} буллит {1}",
			SfwCategory.Cuddle => "{0} тискает {1}",
			SfwCategory.Cry => "{0} плачет",
			SfwCategory.Hug => "{0} обнимает {1}",
			SfwCategory.Kiss => "{0} целует {1}",
			SfwCategory.Lick => "{0} лижет {1}",
			SfwCategory.Pat => "{0} гладит {1}",
			SfwCategory.Smug => "{0} лібится",
			SfwCategory.Bonk => "{0} бонкает {1}",
			SfwCategory.Yeet => "{0} yeet'ит с {1}",
			SfwCategory.Blush => "{0} краснеет",
			SfwCategory.Smile => "{0} улыбается",
			SfwCategory.Wave => "{0} машет",
			SfwCategory.Highfive => "{0} дает пятюню {1}",
			SfwCategory.Handhold => "{0} держит за руку {1}",
			SfwCategory.Nom => "{0} укшоет",
			SfwCategory.Bite => "{0} кусает {1}",
			SfwCategory.Glomp => "{0} сильно обнимает {1}",
			SfwCategory.Slap => "{0} выдает леща {1}",
			SfwCategory.Kill => "{0} убивает {1}",
			SfwCategory.Kick => "{0} пинает {1}",
			SfwCategory.Happy => "{0} веселый",
			SfwCategory.Wink => "{0} подмигивает",
			SfwCategory.Poke => "{0} тыкоет {1}",
			SfwCategory.Dance => "{0} танцует",
			SfwCategory.Cringe => "{0} кринжует",

			SfwCategory.Waifu => "expr",
			SfwCategory.Neko => "expr",
			SfwCategory.Shinobu => "expr",
			SfwCategory.Megumin => "expr",
			SfwCategory.Awoo => "expr",
			_ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
		};
	}

	public class SfwAutocompleteHandler : AutocompleteHandler
	{
		private IEnumerable<AutocompleteResult> m_sfwCategories = Array.Empty<AutocompleteResult>();

		public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
			IInteractionContext context,
			IAutocompleteInteraction autocompleteInteraction,
			IParameterInfo parameter,
			IServiceProvider services)
		{
			try
			{
				if (!m_sfwCategories.Any())
				{
					var res = new AutocompleteResult[31];
					for (var i = 0; i < 31; i++)
					{
						if (!Enum.TryParse<SfwCategory>(i.ToString(), out var category)) continue;

						var name = category.EnumToString();
						res[i] = new AutocompleteResult(name, name);
					}

					m_sfwCategories = res;
				}

				var userInput = autocompleteInteraction.Data.Current.Value.ToString()!;
				var results =
					m_sfwCategories.Where(x =>
						x.Name.StartsWith(userInput,
							StringComparison
								.InvariantCultureIgnoreCase));

				return AutocompletionResult.FromSuccess(results.Take(5));
			}
			catch (Exception e)
			{
				await ClientEvents.OnLog(
					new LogMessage(
						LogSeverity.Error,
						nameof(SfwAutocompleteHandler) + " " + nameof(GenerateSuggestionsAsync),
						e.Message,
						e));
				return AutocompletionResult.FromError(e);
			}
		}
	}
}

public enum CategoryFormat : byte
{
	Neutral,
	Solo,
	User
}

public static class CategoryFormatExtensions
{
	public static string CategoryFormatToString(this CategoryFormat format)
	{
		return format switch
		{
			CategoryFormat.Neutral => nameof(CategoryFormat.Neutral),
			CategoryFormat.Solo => nameof(CategoryFormat.Solo),
			CategoryFormat.User => nameof(CategoryFormat.User),
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
		};
	}
}