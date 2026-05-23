using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CursovoyProjectxDxD.Models
{
    /// <summary>
    /// DTO релиза GitHub с минимальным набором полей.
    /// </summary>
    public sealed class GitHubReleaseDto
    {
        /// <summary>
        /// Тег релиза.
        /// </summary>
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        /// <summary>
        /// Человекочитаемое имя релиза.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Описание релиза.
        /// </summary>
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Список ассетов релиза.
        /// </summary>
        [JsonPropertyName("assets")]
        public List<GitHubAssetDto> Assets { get; set; } = new List<GitHubAssetDto>();
    }
}
