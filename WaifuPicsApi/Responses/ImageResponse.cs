using Newtonsoft.Json;

namespace WaifuPicsApi.Responses;

public class ImageResponse
{
	[JsonProperty("url")] public string Url { get; set; } = "";
}