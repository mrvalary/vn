using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VnInstaller.Models
{
    /// <summary>
    /// DTO релиза GitHub с набором файлов-ассетов.
    /// </summary>
    public sealed class GitHubReleaseDto
    {
        /// <summary>
        /// Тег релиза.
        /// </summary>
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        /// <summary>
        /// Список ассетов релиза.
        /// </summary>
        [JsonPropertyName("assets")]
        public List<GitHubAssetDto> Assets { get; set; }
    }
}
