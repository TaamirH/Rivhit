namespace Rivhit.Api.Attendance;

public sealed class IdempotencyKey
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = "";
    public string Key { get; set; } = "";

    public Guid PunchId { get; set; }
    public Punch? Punch { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

