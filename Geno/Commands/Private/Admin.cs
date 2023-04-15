using Discord;
using Discord.Interactions;
using Geno.Handlers;
using Geno.Responsers.Success;
using Geno.Utils.Types;

namespace Geno.Commands.Private;

[Group("admin", "admin commands")]
[Private(Category.Admin)]
[RequireOwner]
public class Admin : ModuleBase
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
			await Respond(new EmbedBuilder().WithDescription("Registered"), ephemeral: true, isDefered: true);
			return;
		}

		await Respond(new EmbedBuilder().WithDescription("Category not found"), ephemeral: true,
			isDefered: true);
	}
}