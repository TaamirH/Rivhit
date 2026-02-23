using System.Text.Json.Serialization;

namespace Rivhit.Api.Time;

public sealed record TimeApiIoZoneResponse
{
    [JsonPropertyName("timeZone")]
    public string? Timezone { get; init; }

    // Note: timeapi.io returns local time WITHOUT offset, e.g. "2026-02-23T09:10:06.9334249"
    [JsonPropertyName("currentLocalTime")]
    public string? CurrentLocalTime { get; init; }

    [JsonPropertyName("currentUtcOffset")]
    public TimeApiIoOffset? CurrentUtcOffset { get; init; }
}

public sealed record TimeApiIoOffset
{
    [JsonPropertyName("seconds")]
    public int? Seconds { get; init; }
}

