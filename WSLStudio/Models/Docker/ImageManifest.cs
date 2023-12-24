using Newtonsoft.Json;

namespace WSLStudio.Models.Docker;

public class ImageManifest
{
    [JsonProperty("schemaVersion")]
    public int SchemaVersion { get; set; }

    [JsonProperty("mediaType")]
    public string MediaType { get; set; }

    [JsonProperty("config")]
    public Config Config { get; set; }

    [JsonProperty("layers")]
    public List<Config> Layers { get; set; }
}

public class Config
{
    [JsonProperty("mediaType")]
    public string MediaType { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("digest")]
    public string Digest { get; set; }
}
