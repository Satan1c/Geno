using Discord;
using Discord.Interactions;
using Geno.Handlers;
using Geno.Responsers.Success;

namespace Geno.Utils.Types;

public class ModuleBase : InteractionModuleBase<ShardedInteractionContext>
{
	public async ValueTask Log(LogMessage message)
	{
		await ClientEvents.OnLog(message).ConfigureAwait(false);
	}
	
	public async ValueTask Respond(
		EmbedBuilder embed,
		FileAttachment? attachment = null,
		ComponentBuilder? components = null,
		bool ephemeral = false,
		bool isDefered = false,
		bool isFolluwup = false)
	{
		await Context.Interaction.Respond(embed, attachment, components, ephemeral, isDefered, isFolluwup).ConfigureAwait(false);
	}
	public async ValueTask Respond(
		EmbedBuilder[] embeds,
		FileAttachment[]? attachments = null,
		ComponentBuilder? components = null,
		bool ephemeral = false,
		bool isDefered = false,
		bool isFolluwup = false)
	{
		await Context.Interaction.Respond(embeds, attachments, components, ephemeral, isDefered, isFolluwup).ConfigureAwait(false);
	}
}