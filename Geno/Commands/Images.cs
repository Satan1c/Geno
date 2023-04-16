using System.Diagnostics;
using DemotivatorService;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Geno.Handlers;
using Geno.Responsers.Success;
using Geno.Utils.Types;
using WaifuPicsApi;
using WaifuPicsApi.Enums;

namespace Geno.Commands;

[Group("img", "images group")]
public class Images : ModuleBase
{
	private readonly DiscordShardedClient m_client;

	private readonly WaifuClient m_waifuClient;
	//private IEnumerable<AutocompleteResult> m_sfwCategories = Array.Empty<AutocompleteResult>();

	public Images(DiscordShardedClient client, WaifuClient waifuClient)
	{
		m_client = client;
		m_waifuClient = waifuClient;
	}

	[SlashCommand("demotivator", "generate demotivator")]
	public async Task Demotivator(IAttachment? attachment = null,
		string? upperText = "Текст",
		string? lowerText = "Текст")
	{
		var url = attachment?.Url ??
		          Context.Guild?.GetUser(Context.User.Id).GetDisplayAvatarUrl(ImageFormat.Png, 512) ??
		          Context.User.GetAvatarUrl(ImageFormat.Png, 512);
		
		var clock = Stopwatch.StartNew();
		
		var generator = new DemotivatorGenerator(url, upperText, lowerText);
		var file = generator.GetResult();
		
		clock.Stop();
		await Log(new LogMessage(LogSeverity.Verbose, $"{nameof(Images)}.{nameof(Demotivator)}",
			$"Generation elapsed time: {clock.ElapsedMilliseconds.ToString()}ms"));
		clock.Reset();
		
		var ids = new[] { "finish", "add", "add_text" };

		await Respond(new EmbedBuilder().WithImageUrl("attachment://demotivator.png"),
			file,
			new ComponentBuilder().AddRow(new ActionRowBuilder()
				.WithButton("Finish", ids[0], ButtonStyle.Success)
				.WithButton("Add text", ids[1])
			),
			true,
			true);

        var closeAt = DateTimeOffset.UtcNow.AddMinutes(1);
		var timeout = TimeSpan.FromMinutes(1);

		while (DateTimeOffset.UtcNow < closeAt)
		{
			var interaction =
				await InteractionUtility.WaitForInteractionAsync(m_client, timeout,
					interaction => interaction is IComponentInteraction or IModalInteraction);

			if (interaction is IComponentInteraction buttonInteraction)
			{
				var id = buttonInteraction.Data.CustomId;
				if (id == ids[0] || id != ids[1])
				{
					await buttonInteraction.DeferAsync();

					file = generator.GetResult();
					await buttonInteraction.Respond(new EmbedBuilder().WithImageUrl("attachment://demotivator.png"), file, new ComponentBuilder(), false, true);

					break;
				}

				closeAt = DateTimeOffset.UtcNow.AddMinutes(1);
				await interaction.RespondWithModalAsync<DemotivatorTextModal>(ids[2]);
			}
			else if (interaction is IModalInteraction modalInteraction)
			{
				var id = modalInteraction.Data.CustomId;
				if (id != "add_text") continue;

				await modalInteraction.DeferAsync();

				closeAt = DateTimeOffset.UtcNow.AddMinutes(1);
				var comps = modalInteraction.Data.Components.ToArray();
			}
		}

		generator.Dispose();
	}

	[SlashCommand("nsfw", "nsfw images")]
	[RequireNsfw]
	public async Task NsfwCommands(NsfwCategory tag)
	{
		var img = await m_waifuClient.GetImageAsync(tag);
		Console.WriteLine(img);

		await Respond(embed: new EmbedBuilder().WithImageUrl(img));
	}

	[SlashCommand("sfw", "sfw images")]
	public async Task SfwCommands([Autocomplete(typeof(SfwAutocompleteHandler))] string tag, IUser? user = null)
	{
		try
		{
			if (!Enum.TryParse<SfwCategory>(tag, out var category)) return;

			var ctf = GetCategoryFormat(category);
			if (ctf == CategoryFormat.User && user == null)
			{
				throw new ArgumentException($"You must provide {nameof(user)} for this category");
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

			await Respond(embed: embed.WithDescription(title).WithImageUrl(img));
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
			_ => CategoryFormat.Neutral
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

			SfwCategory.Waifu => nameof(SfwCategory.Waifu),
			SfwCategory.Neko => nameof(SfwCategory.Neko),
			SfwCategory.Shinobu => nameof(SfwCategory.Shinobu),
			SfwCategory.Megumin => nameof(SfwCategory.Megumin),
			SfwCategory.Awoo => nameof(SfwCategory.Awoo),
			_ => ""
		};
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
			_ => ""
		};
	}
}