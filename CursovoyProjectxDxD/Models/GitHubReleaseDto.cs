using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CursovoyProjectxDxD.Models
{
    // DTO релиза GitHub с минимальным набором полей.
    public sealed class GitHubReleaseDto
    {
        // Тег релиза.
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        // Человекочитаемое имя релиза.
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // Описание релиза.
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        // Список ассетов релиза.
        [JsonPropertyName("assets")]
        public List<GitHubAssetDto> Assets { get; set; } = new List<GitHubAssetDto>();
    }
}
