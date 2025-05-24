using System.Text.Json.Serialization;

namespace Robust.LanguageServer.Notifications
{
    public record ProgressInfo
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
