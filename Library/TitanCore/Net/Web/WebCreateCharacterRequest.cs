using Newtonsoft.Json;

public class WebCharacterJson
{
    [JsonProperty("nftId")]
    public ulong nftId { get; set; }

    [JsonProperty("accountId")]
    public string accountId { get; set; }

    [JsonProperty("type")]
    public ushort type { get; set; }

    [JsonProperty("skin")]
    public ushort skin { get; set; }
}