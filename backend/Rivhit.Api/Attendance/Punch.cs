using Rivhit.Api.Time;

namespace Rivhit.Api.Attendance;

public sealed class Punch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = "";

    public Guid ShiftId { get; set; }
    public Shift? Shift { get; set; }

    public PunchType Type { get; set; }

    // Who initiated this punch: employee (self-service) or admin (on behalf).
    public string CreatedByUserId { get; set; } = "";
    public bool CreatedByIsAdmin { get; set; }
    public string? Note { get; set; }

    // Authoritative time (always sourced from external API)
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset OccurredAtZurich { get; set; }
    public long UnixTimeSeconds { get; set; }
    public string Timezone { get; set; } = "Europe/Zurich";
    public string UtcOffset { get; set; } = "";

    // Raw response from the time API (tamper-evidence / auditability)
    public string TimeApiRawResponse { get; set; } = "";

    public static Punch FromSnapshot(
        string userId,
        Guid shiftId,
        PunchType type,
        ZurichTimeSnapshot snapshot,
        string createdByUserId,
        bool createdByIsAdmin,
        string? note) =>
        new()
        {
            UserId = userId,
            ShiftId = shiftId,
            Type = type,
            CreatedByUserId = createdByUserId,
            CreatedByIsAdmin = createdByIsAdmin,
            Note = note,
            OccurredAtUtc = snapshot.UtcDateTime,
            OccurredAtZurich = snapshot.ZurichDateTime,
            UnixTimeSeconds = snapshot.UnixTimeSeconds,
            Timezone = snapshot.Timezone,
            UtcOffset = snapshot.UtcOffset,
            TimeApiRawResponse = snapshot.RawResponse
        };
}

