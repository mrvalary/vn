using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VnInstaller.Models
{
    // DTO релиза GitHub с набором файлов-ассетов.
    public sealed class GitHubReleaseDto
    {
        // Тег релиза.
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        // Список ассетов релиза.
        [JsonPropertyName("assets")]
        public List<GitHubAssetDto> Assets { get; set; }
    }
}
