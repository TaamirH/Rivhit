namespace Rivhit.Api.Attendance;

public sealed class Shift
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = "";

    // Open/close are authoritative times, stored as UTC + local Zurich.
    public DateTimeOffset OpenedAtUtc { get; set; }
    public DateTimeOffset OpenedAtZurich { get; set; }

    public DateTimeOffset? ClosedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtZurich { get; set; }

    public Guid ClockInPunchId { get; set; }

    public Guid? ClockOutPunchId { get; set; }

    public List<Punch> Punches { get; set; } = [];
}

