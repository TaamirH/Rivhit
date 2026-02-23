using System.Net.Http.Headers;
using System.Text.Json;

namespace Rivhit.Api.Time;

public sealed class WorldTimeApiTimeProvider(HttpClient httpClient) : ITimeProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ZurichTimeSnapshot> GetZurichNowAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "Europe/Zurich");
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
        WorldTimeApiResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<WorldTimeApiResponse>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            throw new TimeProviderException("Failed to parse WorldTimeAPI response JSON.", ex);
        }

        if (parsed?.UtcDateTime is null ||
            parsed.ZurichDateTime is null ||
            parsed.UnixTimeSeconds is null ||
            string.IsNullOrWhiteSpace(parsed.Timezone) ||
            string.IsNullOrWhiteSpace(parsed.UtcOffset))
        {
            throw new TimeProviderException("WorldTimeAPI response is missing required fields.");
        }

        return new ZurichTimeSnapshot(
            UtcDateTime: parsed.UtcDateTime.Value,
            ZurichDateTime: parsed.ZurichDateTime.Value,
            UnixTimeSeconds: parsed.UnixTimeSeconds.Value,
            Timezone: parsed.Timezone!,
            UtcOffset: parsed.UtcOffset!,
            RawResponse: raw
        );
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

