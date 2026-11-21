using System.Text.Json.Serialization;

namespace FeedbackETL.Domain.Entities;

public sealed class ApiComment
{
    [JsonPropertyName("postId")]
    public int PostId { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}
