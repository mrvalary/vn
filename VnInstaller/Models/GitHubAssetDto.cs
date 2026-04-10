using System.Text.Json.Serialization;

namespace VnInstaller.Models
{
    // DTO одного asset-файла из GitHub Releases API.
    public sealed class GitHubAssetDto
    {
        // Имя файла.
        [JsonPropertyName("name")]
        public string Name { get; set; }

        // URL прямого скачивания файла.
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }
}
