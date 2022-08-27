using System.Net.Http.Headers;
using Newtonsoft.Json;
using WaifuPicsApi.Enums;
using WaifuPicsApi.Responses;
using Type = WaifuPicsApi.Enums.Type;

namespace WaifuPicsApi;

public class WaifuClient
{
    private readonly HttpClient m_client = new HttpClient();

    public WaifuClient()
    {
        m_client.BaseAddress = new Uri("https://api.waifu.pics/");
        m_client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );
    }
    
    private async Task<string> Request(Type type, string category)
    {
        var response = await m_client.GetAsync($"{type.EnumToString().ToLower()}/{category}");
        response.EnsureSuccessStatusCode();
        return JsonConvert.DeserializeObject<ImageResponse>(await response.Content.ReadAsStringAsync())!.Url;
    }

    public async Task<string> GetImageAsync(SfwCategory category) 
        => await Request(Type.Sfw, category.EnumToString().ToLower());

    public async Task<string> GetImageAsync(NsfwCategory category) 
        => await Request(Type.Nsfw, category.EnumToString().ToLower());
}