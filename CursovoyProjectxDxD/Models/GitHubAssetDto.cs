using System.Text.Json.Serialization;

namespace CursovoyProjectxDxD.Models
{
    /// <summary>
    /// DTO одного asset-файла из ответа GitHub Releases API.
    /// </summary>
    public sealed class GitHubAssetDto
    {
        /// <summary>
        /// Имя файла в релизе.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL прямого скачивания файла.
        /// </summary>
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
