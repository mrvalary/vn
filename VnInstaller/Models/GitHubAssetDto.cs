using System.Text.Json.Serialization;

namespace VnInstaller.Models
{
    /// <summary>
    /// DTO одного asset-файла из GitHub Releases API.
    /// </summary>
    public sealed class GitHubAssetDto
    {
        /// <summary>
        /// Имя файла.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// URL прямого скачивания файла.
        /// </summary>
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }
}
