using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Rivhit.Api.Audit;
using Rivhit.Api.Data;
using Rivhit.Api.Time;

namespace Rivhit.Api.Attendance;

public sealed class AdminShiftService(AppDbContext db, ITimeProvider timeProvider)
{
    public async Task<ShiftDto> CloseShiftAsync(string adminUserId, Guid shiftId, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Reason is required.");
        }

        var snapshot = await timeProvider.GetZurichNowAsync(ct);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var shift = await db.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId, ct);
        if (shift is null)
        {
            throw new InvalidOperationException("Shift not found.");
        }

        if (shift.ClosedAtUtc is not null)
        {
            throw new InvalidOperationException("Shift is already closed.");
        }

        var punch = Punch.FromSnapshot(
            userId: shift.UserId,
            shiftId: shift.Id,
            type: PunchType.ClockOut,
            snapshot: snapshot,
            createdByUserId: adminUserId,
            createdByIsAdmin: true,
            note: reason);

        shift.ClockOutPunchId = punch.Id;
        shift.ClosedAtUtc = snapshot.UtcDateTime;
        shift.ClosedAtZurich = snapshot.ZurichDateTime;

        db.Punches.Add(punch);
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminUserId,
            Action = "AdminCloseShift",
            TargetUserId = shift.UserId,
            TargetShiftId = shift.Id,
            TargetPunchId = punch.Id,
            DetailsJson = JsonSerializer.Serialize(new
            {
                Reason = reason,
                snapshot.UtcDateTime,
                snapshot.ZurichDateTime,
                snapshot.Timezone,
                snapshot.UtcOffset
            })
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var punches = await db.Punches
            .AsNoTracking()
            .Where(p => p.ShiftId == shift.Id)
            .OrderBy(p => p.UnixTimeSeconds)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

        return ShiftDto.From(shift, punches.Select(PunchDto.From).ToArray());
    }

    public async Task<IReadOnlyList<AdminOpenShiftDto>> ListOpenShiftsAsync(CancellationToken ct)
    {
        // Joining Identity users directly is fine for this take-home.
        var query =
            from s in db.Shifts.AsNoTracking()
            join u in db.Users.AsNoTracking() on s.UserId equals u.Id
            where s.ClosedAtUtc == null
            orderby s.OpenedAtUtc
            select new AdminOpenShiftDto(
                s.Id,
                s.UserId,
                u.Email ?? "",
                s.OpenedAtUtc,
                s.OpenedAtZurich
            );

        return await query.Take(200).ToListAsync(ct);
    }
}

public sealed record AdminOpenShiftDto(
    Guid ShiftId,
    string UserId,
    string Email,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset OpenedAtZurich
);

