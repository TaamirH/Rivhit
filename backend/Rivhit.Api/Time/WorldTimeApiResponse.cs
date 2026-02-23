using System.Text.Json.Serialization;

namespace Rivhit.Api.Time;

// Minimal subset of https://worldtimeapi.org/ response fields that we persist for auditability.
public sealed record WorldTimeApiResponse
{
    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    [JsonPropertyName("utc_datetime")]
    public DateTimeOffset? UtcDateTime { get; init; }

    [JsonPropertyName("datetime")]
    public DateTimeOffset? ZurichDateTime { get; init; }

    [JsonPropertyName("utc_offset")]
    public string? UtcOffset { get; init; }

    [JsonPropertyName("unixtime")]
    public long? UnixTimeSeconds { get; init; }
}

