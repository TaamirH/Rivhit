namespace Rivhit.Api.Audit;

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string ActorUserId { get; set; } = "";
    public string Action { get; set; } = "";

    public string? TargetUserId { get; set; }
    public Guid? TargetShiftId { get; set; }
    public Guid? TargetPunchId { get; set; }

    public string DetailsJson { get; set; } = "{}";
}

