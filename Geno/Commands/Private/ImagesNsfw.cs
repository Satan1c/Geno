using Discord;
using Discord.Interactions;
using Geno.Utils.Types;
using WaifuPicsApi;
using WaifuPicsApi.Enums;

namespace Geno.Commands.Private;

[Group("img", "images group")]
[Private(Category.Images)]
public class ImagesNsfw : ModuleBase
{
	private readonly WaifuClient m_waifuClient;

	public ImagesNsfw(WaifuClient waifuClient)
	{
		m_waifuClient = waifuClient;
	}

	[SlashCommand("nsfw", "nsfw images")]
	[RequireNsfw]
	public async Task NsfwCommands(NsfwCategory tag)
	{
		var img = await m_waifuClient.GetImageAsync(tag);
		Console.WriteLine(img);

		await Respond(new EmbedBuilder().WithImageUrl(img));
	}
}