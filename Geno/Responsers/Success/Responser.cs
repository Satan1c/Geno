using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Discord;
using Geno.Responsers.Success.Modules;
using Localization;

namespace Geno.Responsers.Success;

public static class Responser
{
	public static void Init(LocalizationManager localizationManager)
	{
		GenshinResponse.Init(localizationManager);
		GenshinResponse.Init(localizationManager);
		GenshinResponse.Init(localizationManager);
	}

	public static ValueTask Respond(this IDiscordInteraction interaction,
		EmbedBuilder embed,
		FileAttachment? attachment = null,
		ComponentBuilder? components = null,
		bool ephemeral = false,
		bool isDefered = false,
		bool isFolluwup = false)
	{
		return interaction.Respond(embed.WithColor(embed.Color ?? new Color(43, 45, 49)).Build(),
			ephemeral,
			isDefered,
			isFolluwup,
			null,
			components?.Build(),
			attachment);
	}

	public static ValueTask Respond(this IDiscordInteraction interaction,
		EmbedBuilder[] embeds,
		FileAttachment[]? attachments = null,
		ComponentBuilder? components = null,
		bool ephemeral = false,
		bool isDefered = false,
		bool isFolluwup = false)
	{
		var res = new Embed[embeds.Length];
		ref var start = ref MemoryMarshal.GetArrayDataReference(embeds);
		ref var resStart = ref MemoryMarshal.GetArrayDataReference(res);
		ref var end = ref Unsafe.Add(ref start, embeds.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			resStart = start.WithColor(start.Color ?? new Color(43, 45, 49)).Build();

			start = ref Unsafe.Add(ref start, 1);
			resStart = ref Unsafe.Add(ref resStart, 1);
		}

		return interaction.Respond(null, ephemeral, isDefered, isFolluwup, res, components?.Build(),
			attachments: attachments);
	}

	private static async ValueTask Respond(this IDiscordInteraction interaction,
		Embed? embed = null,
		bool ephemeral = false,
		bool isDefered = false,
		bool isFolluwup = false,
		Embed[]? embeds = null,
		MessageComponent? components = null,
		FileAttachment? attachment = null,
		FileAttachment[]? attachments = null)
	{
		if (isFolluwup)
		{
			if (attachments is { Length: > 0 })
			{
				await interaction.FollowupWithFilesAsync(
						attachments,
						embed: embed,
						embeds: embeds,
						allowedMentions: AllowedMentions.None,
						components: components,
						ephemeral: ephemeral)
					.ConfigureAwait(false);
				return;
			}

			if (attachment is not null)
			{
				await interaction.FollowupWithFileAsync(
						attachment.Value,
						embed: embed,
						embeds: embeds,
						components: components,
						allowedMentions: AllowedMentions.None,
						ephemeral: ephemeral)
					.ConfigureAwait(false);
				return;
			}
			
			await interaction.FollowupAsync(
					embed: embed,
					embeds: embeds,
					components: components,
					allowedMentions: AllowedMentions.None,
					ephemeral: ephemeral)
				.ConfigureAwait(false);
			return;
		}

		if (isDefered)
		{
			await interaction.ModifyOriginalResponseAsync(x =>
				{
					if (embed is not null)
						x.Embed = embed;
					
					if (embeds is not null)
						x.Embeds = embeds;
					
					x.AllowedMentions = AllowedMentions.None;
					
					if (attachment is not null || attachments is not null)
						x.Attachments = attachment.HasValue ? new[] { attachment.Value } : attachments;
					
					if (components is not null)
						x.Components = components;
				}
			).ConfigureAwait(false);

			return;
		}

		try
		{
			if (attachments is { Length: > 0 })
			{
				await interaction.RespondWithFilesAsync(
						attachments,
						embed: embed,
						embeds: embeds,
						allowedMentions: AllowedMentions.None,
						components: components,
						ephemeral: ephemeral)
					.ConfigureAwait(false);
				return;
			}

			if (attachment is not null)
			{
				await interaction.RespondWithFileAsync(
						attachment.Value,
						embed: embed,
						embeds: embeds,
						components: components,
						allowedMentions: AllowedMentions.None,
						ephemeral: ephemeral)
					.ConfigureAwait(false);
				return;
			}

			await interaction.RespondAsync(
				embed: embed,
				embeds: embeds,
				allowedMentions: AllowedMentions.None,
				components: components,
				ephemeral: ephemeral
			).ConfigureAwait(false);
		}
		catch (InvalidOperationException _)
		{
			await interaction.Respond(embed, ephemeral, true, isFolluwup, embeds, components, attachment, attachments)
				.ConfigureAwait(false);
		}
	}
}