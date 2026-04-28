using System.Text.Json.Serialization;

namespace CursovoyProjectxDxD.Models
{
    // DTO одного asset-файла из ответа GitHub Releases API.
    public sealed class GitHubAssetDto
    {
        // Имя файла в релизе.
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // URL прямого скачивания файла.
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
