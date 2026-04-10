using System.Text.Json.Serialization;

namespace VnInstaller.Models
{
    public sealed class GitHubAssetDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }
}
