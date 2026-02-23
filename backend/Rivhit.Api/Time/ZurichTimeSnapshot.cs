namespace Rivhit.Api.Time;

public sealed record ZurichTimeSnapshot(
    DateTimeOffset UtcDateTime,
    DateTimeOffset ZurichDateTime,
    long UnixTimeSeconds,
    string Timezone,
    string UtcOffset,
    string RawResponse
);

