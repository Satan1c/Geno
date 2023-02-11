using Discord;
using Discord.Interactions;
using Geno.Responses;
using Geno.Utils.Services;
using Geno.Utils.Types;

namespace Geno.Commands.Private;

[Group("admin", "admin commands")]
[Private(Category.Admin)]
[RequireOwner]
public class Admin : InteractionModuleBase<ShardedInteractionContext>
{
	[SlashCommand("reg_category", "slash categories registration")]
	[RequireOwner]
	public async Task Registration(ulong guild, Category category, bool clear = false)
	{
		await DeferAsync(true);

		clear = category == Category.None || clear;

		if (CommandHandlingService.Private.TryGetValue(category, out var modules))
		{
			await CommandHandlingService.Interactions.AddModulesToGuildAsync(guild, clear, modules);

			await Context.Respond(new EmbedBuilder().WithColor(Color.Green).WithDescription("Registered"), true, true);
			return;
		}

		await Context.Respond(new EmbedBuilder().WithColor(Color.Red).WithDescription("Category not found"), true,
			true);
	}
}