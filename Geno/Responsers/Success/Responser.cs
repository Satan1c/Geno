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

	public static ValueTask Respond(this IInteractionContext context,
		EmbedBuilder embed,
		FileAttachment? attachment = null,
		ComponentBuilder? components = null,
		bool ephemeral = false,
		bool isDefered = false,
		bool isFolluwup = false)
	{
		return context.Respond(embed.WithColor(embed.Color ?? new Color(43, 45, 49)).Build(),
			ephemeral,
			isDefered,
			isFolluwup,
			null,
			components?.Build(),
			attachment);
	}

	public static ValueTask Respond(this IInteractionContext context,
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

		return context.Respond(null, ephemeral, isDefered, isFolluwup, res, components?.Build(),
			attachments: attachments);
	}

	private static async ValueTask Respond(this IInteractionContext context,
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
				await context.Interaction.FollowupWithFilesAsync(
						attachments,
						embed: embed,
						embeds: embeds,
						allowedMentions: AllowedMentions.None,
						components: components,
						ephemeral: ephemeral)
					.ConfigureAwait(false);
				return;
			}

			if (attachment is { })
			{
				await context.Interaction.FollowupWithFileAsync(
						attachment.Value,
						embed: embed,
						embeds: embeds,
						components: components,
						allowedMentions: AllowedMentions.None,
						ephemeral: ephemeral)
					.ConfigureAwait(false);
				return;
			}
		}

		if (isDefered)
		{
			await context.Interaction.ModifyOriginalResponseAsync(x =>
				{
					var flags = x.Flags.GetValueOrDefault() ?? MessageFlags.None;
					x.Embed = embed;
					x.Embeds = embeds;
					x.AllowedMentions = AllowedMentions.None;
					x.Flags = ephemeral ? flags ^ MessageFlags.Ephemeral : flags | MessageFlags.Ephemeral;
					x.Attachments = attachment.HasValue ? new[] { attachment.Value } : attachments;
					x.Components = components;
				}
			).ConfigureAwait(false);

			return;
		}

		try
		{
			if (attachments is { Length: > 0 })
			{
				await context.Interaction.RespondWithFilesAsync(
						attachments,
						embed: embed,
						embeds: embeds,
						allowedMentions: AllowedMentions.None,
						components: components,
						ephemeral: ephemeral)
					.ConfigureAwait(false);
				return;
			}

			if (attachment is { })
			{
				await context.Interaction.RespondWithFileAsync(
						attachment.Value,
						embed: embed,
						embeds: embeds,
						components: components,
						allowedMentions: AllowedMentions.None,
						ephemeral: ephemeral)
					.ConfigureAwait(false);
				return;
			}

			await context.Interaction.RespondAsync(
				embed: embed,
				embeds: embeds,
				allowedMentions: AllowedMentions.None,
				components: components,
				ephemeral: ephemeral
			).ConfigureAwait(false);
		}
		catch (InvalidOperationException _)
		{
			await context.Respond(embed, ephemeral, true, isFolluwup, embeds, components, attachment, attachments)
				.ConfigureAwait(false);
		}
	}
}