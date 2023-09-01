using Newtonsoft.Json;

namespace WSLStudio.Models.Docker;

public class AuthToken
{
    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("expires_in")]
    public long ExpiresIn { get; set; }

    [JsonProperty("issued_at")]
    public DateTimeOffset IssuedAt { get; set; }
}