using Discord;
using Discord.Interactions;
using Localization;

namespace Geno.Responsers.Error;

public interface IErrorResolver
{
	public string ModuleName { get; }
	public LocalizationManager localizationManager { get; set; }
	public EmbedBuilder Resolve(IResult result, ICommandInfo command, IInteractionContext context, EmbedBuilder embed);
}