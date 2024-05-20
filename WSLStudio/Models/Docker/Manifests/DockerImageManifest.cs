using Newtonsoft.Json;
using WSLStudio.Contracts.Models.Docker.Manifests;

namespace WSLStudio.Models.Docker.Manifests;

public class DockerImageManifest : IImageManifest
{
    [JsonProperty("schemaVersion")]
    public int SchemaVersion
    {
        get; set;
    }

    [JsonProperty("mediaType")]
    public string MediaType
    {
        get; set;
    }

    [JsonProperty("config")]
    public Config Config
    {
        get; set;
    }

    [JsonProperty("layers")]
    public List<Config> Layers
    {
        get; set;
    }

    public List<string> GetLayers()
    {
        return Layers.Select(layer => layer.Digest).ToList();
    }
}

public class Config
{
    [JsonProperty("mediaType")]
    public string MediaType
    {
        get; set;
    }

    [JsonProperty("size")]
    public int Size
    {
        get; set;
    }

    [JsonProperty("digest")]
    public string Digest
    {
        get; set;
    }
}
