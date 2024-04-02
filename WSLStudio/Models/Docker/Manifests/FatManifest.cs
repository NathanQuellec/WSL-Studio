using Newtonsoft.Json;

namespace WSLStudio.Models.Docker.Manifests;

public class FatManifest : IImageManifest
{
    [JsonProperty("manifests")]
    public List<Manifest> manifests { get; set; }

    [JsonProperty("mediaType")]
    public string mediaType { get; set; }

    [JsonProperty("schemaVersion")]
    public int schemaVersion { get; set; }

    public List<string> getLayers()
    {
        return manifests.Select(manifest => manifest.digest).ToList();
    }

    public string getAmd64ManifestDigest()
    {
        return manifests.First(manifest => manifest.platform.architecture == "amd64").digest;
    }
}

public class Manifest
{
    [JsonProperty("digest")]
    public string digest { get; set; }

    [JsonProperty("mediaType")]
    public string mediaType { get; set; }

    [JsonProperty("platform")]
    public Platform platform { get; set; }

    [JsonProperty("size")]
    public int size { get; set; }
}

public class Platform
{
    [JsonProperty("architecture")]
    public string architecture { get; set; }

    [JsonProperty("os")]
    public string os { get; set; }

    [JsonProperty("variant")]
    public string variant { get; set; }
}