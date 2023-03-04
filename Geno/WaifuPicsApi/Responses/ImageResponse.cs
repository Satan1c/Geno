using Newtonsoft.Json;

namespace Geno.WaifuPicsApi.Responses;

public class ImageResponse
{
	[JsonProperty("url")] public string Url { get; set; } = "";
}