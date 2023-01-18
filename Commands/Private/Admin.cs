using Discord.Interactions;
using Geno.Utils;

namespace Geno.Commands.Private;

[Group("admin", "admin commands")]
[Private(Category.Admin)]
public class Admin : InteractionModuleBase<ShardedInteractionContext>
{
	[SlashCommand("reg_category", "slash categories registration")]
	public async Task Registration(ulong guild, Category category, bool clear = false)
	{
		if (CommandHandlingService.Private.TryGetValue(category, out var modules))
			await CommandHandlingService.Interactions.AddModulesToGuildAsync(guild, clear, modules);
	}
}