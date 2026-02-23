using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Rivhit.Api.Time;

// Uses timeapi.io because some networks block worldtimeapi.org TLS handshakes.
public sealed class TimeApiIoTimeProvider(HttpClient httpClient) : ITimeProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ZurichTimeSnapshot> GetZurichNowAsync(CancellationToken cancellationToken)
    {
        // Docs: https://timeapi.io/swagger/index.html (TimeZone -> /api/TimeZone/zone)
        var url = "TimeZone/zone?timeZone=Europe/Zurich";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadBodyAsync(response, cancellationToken);
            throw new TimeProviderException(
                $"Time provider returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}"
            );
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        TimeApiIoZoneResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<TimeApiIoZoneResponse>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            throw new TimeProviderException("Failed to parse timeapi.io response JSON.", ex);
        }

        if (parsed is null ||
            string.IsNullOrWhiteSpace(parsed.Timezone) ||
            string.IsNullOrWhiteSpace(parsed.CurrentLocalTime) ||
            parsed.CurrentUtcOffset?.Seconds is null)
        {
            throw new TimeProviderException("timeapi.io response is missing required fields.");
        }

        // Parse an ISO-8601-like local timestamp with fractional seconds, no offset.
        if (!DateTime.TryParse(
                parsed.CurrentLocalTime,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                out var localDateTime))
        {
            throw new TimeProviderException("Failed to parse timeapi.io currentLocalTime.");
        }

        // Offset is authoritative (from the external API), so UTC conversion is deterministic.
        var offset = TimeSpan.FromSeconds(parsed.CurrentUtcOffset.Seconds.Value);
        var zurich = new DateTimeOffset(
            localDateTime.Year,
            localDateTime.Month,
            localDateTime.Day,
            localDateTime.Hour,
            localDateTime.Minute,
            localDateTime.Second,
            localDateTime.Millisecond,
            offset
        );

        var utc = zurich.ToUniversalTime();

        return new ZurichTimeSnapshot(
            UtcDateTime: utc,
            ZurichDateTime: zurich,
            UnixTimeSeconds: utc.ToUnixTimeSeconds(),
            Timezone: parsed.Timezone!,
            UtcOffset: FormatOffset(offset),
            RawResponse: raw
        );
    }

    private static string FormatOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        offset = offset.Duration();
        return $"{sign}{offset:hh\\:mm}";
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return "<failed to read body>";
        }
    }
}

