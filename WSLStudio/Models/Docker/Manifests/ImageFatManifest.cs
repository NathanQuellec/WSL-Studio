using Newtonsoft.Json;

namespace WSLStudio.Models.Docker.Manifests.impl;

public class ImageFatManifest : IImageManifest
{
    [JsonProperty("manifests")]
    public List<Manifest> Manifests { get; set; }

    [JsonProperty("mediaType")]
    public string MediaType { get; set; }

    [JsonProperty("schemaVersion")]
    public int SchemaVersion { get; set; }

    public List<string> GetLayers() => Manifests.Select(manifest => manifest.Digest).ToList();

    public string GetManifestByArchitecture(string architecture) => Manifests.First(manifest => manifest.Platform.Architecture.Equals(architecture)).Digest;

}

public class Manifest
{
    [JsonProperty("digest")]
    public string Digest { get; set; }

    [JsonProperty("mediaType")]
    public string MediaType { get; set; }

    [JsonProperty("platform")]
    public Platform Platform { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }
}

public class Platform
{
    [JsonProperty("architecture")]
    public string Architecture { get; set; }

    [JsonProperty("os")]
    public string Os { get; set; }

    [JsonProperty("variant")]
    public string Variant { get; set; }
}