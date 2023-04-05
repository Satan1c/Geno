using System.Net.Http.Headers;
using Newtonsoft.Json;
using WaifuPicsApi.Enums;
using WaifuPicsApi.Responses;
using Type = WaifuPicsApi.Enums.Type;

namespace WaifuPicsApi;

public class WaifuClient
{
	private readonly HttpClient m_client = new();

	public WaifuClient()
	{
		m_client.BaseAddress = new Uri("https://api.waifu.pics/");
		m_client.DefaultRequestHeaders.Accept.Add(
			new MediaTypeWithQualityHeaderValue("application/json")
		);
	}

	private async ValueTask<string> Request(Type type, string category)
	{
		var response = await m_client.GetAsync($"{type.EnumToString().ToLower()}/{category}");
		response.EnsureSuccessStatusCode();
		return JsonConvert.DeserializeObject<ImageResponse>(await response.Content.ReadAsStringAsync())!.Url;
	}

	public ValueTask<string> GetImageAsync(SfwCategory category)
	{
		return Request(Type.Sfw, category.EnumToString().ToLower());
	}

	public ValueTask<string> GetImageAsync(NsfwCategory category)
	{
		return Request(Type.Nsfw, category.EnumToString().ToLower());
	}
}