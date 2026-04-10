using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VnInstaller.Models
{
    public sealed class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAssetDto> Assets { get; set; }
    }
}
