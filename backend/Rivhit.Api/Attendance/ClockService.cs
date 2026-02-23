using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Rivhit.Api.Audit;
using Rivhit.Api.Data;
using Rivhit.Api.Time;

namespace Rivhit.Api.Attendance;

public sealed class ClockService(AppDbContext db, ITimeProvider timeProvider)
{
    public async Task<ShiftDto> ClockInAsync(string userId, string? idempotencyKey, CancellationToken ct)
    {
        var existing = await TryGetIdempotentResultAsync(userId, idempotencyKey, ct);
        if (existing is not null)
        {
            return existing;
        }

        // No fallback allowed: if we can't fetch authoritative time, we fail the punch.
        var snapshot = await timeProvider.GetZurichNowAsync(ct);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var openShiftExists = await db.Shifts.AnyAsync(s => s.UserId == userId && s.ClosedAtUtc == null, ct);
        if (openShiftExists)
        {
            throw new InvalidOperationException("Cannot clock in while a shift is already open.");
        }

        var shift = new Shift
        {
            UserId = userId,
            OpenedAtUtc = snapshot.UtcDateTime,
            OpenedAtZurich = snapshot.ZurichDateTime,
        };

        var punch = Punch.FromSnapshot(
            userId,
            shift.Id,
            PunchType.ClockIn,
            snapshot,
            createdByUserId: userId,
            createdByIsAdmin: false,
            note: null);
        shift.ClockInPunchId = punch.Id;

        db.Shifts.Add(shift);
        db.Punches.Add(punch);
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            TargetUserId = userId,
            TargetShiftId = shift.Id,
            TargetPunchId = punch.Id,
            Action = "ClockIn",
            DetailsJson = JsonSerializer.Serialize(new
            {
                snapshot.UtcDateTime,
                snapshot.ZurichDateTime,
                snapshot.Timezone,
                snapshot.UtcOffset
            })
        });

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            db.IdempotencyKeys.Add(new IdempotencyKey
            {
                UserId = userId,
                Key = idempotencyKey,
                PunchId = punch.Id
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return ShiftDto.From(shift, [PunchDto.From(punch)]);
    }

    public async Task<ShiftDto> ClockOutAsync(string userId, string? idempotencyKey, CancellationToken ct)
    {
        var existing = await TryGetIdempotentResultAsync(userId, idempotencyKey, ct);
        if (existing is not null)
        {
            return existing;
        }

        var snapshot = await timeProvider.GetZurichNowAsync(ct);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var shift = await db.Shifts
            .Include(s => s.Punches)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ClosedAtUtc == null, ct);

        if (shift is null)
        {
            throw new InvalidOperationException("Cannot clock out because there is no open shift.");
        }

        var punch = Punch.FromSnapshot(
            userId,
            shift.Id,
            PunchType.ClockOut,
            snapshot,
            createdByUserId: userId,
            createdByIsAdmin: false,
            note: null);
        shift.ClockOutPunchId = punch.Id;
        shift.ClosedAtUtc = snapshot.UtcDateTime;
        shift.ClosedAtZurich = snapshot.ZurichDateTime;

        db.Punches.Add(punch);
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            TargetUserId = userId,
            TargetShiftId = shift.Id,
            TargetPunchId = punch.Id,
            Action = "ClockOut",
            DetailsJson = JsonSerializer.Serialize(new
            {
                snapshot.UtcDateTime,
                snapshot.ZurichDateTime,
                snapshot.Timezone,
                snapshot.UtcOffset
            })
        });

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            db.IdempotencyKeys.Add(new IdempotencyKey
            {
                UserId = userId,
                Key = idempotencyKey,
                PunchId = punch.Id
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Re-query punches in a stable order (avoids duplicates from EF tracking).
        var punches = await db.Punches
            .AsNoTracking()
            .Where(p => p.ShiftId == shift.Id && p.UserId == userId)
            .OrderBy(p => p.UnixTimeSeconds)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

        return ShiftDto.From(shift, punches.Select(PunchDto.From).ToArray());
    }

    public async Task<StatusDto> GetStatusAsync(string userId, CancellationToken ct)
    {
        var openShift = await db.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ClosedAtUtc == null, ct);

        if (openShift is null)
        {
            return new StatusDto(null);
        }

        return new StatusDto(new OpenShiftDto(openShift.Id, openShift.OpenedAtUtc, openShift.OpenedAtZurich));
    }

    public async Task<IReadOnlyList<ShiftDto>> ListMyShiftsAsync(string userId, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken ct)
    {
        var query = db.Shifts
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Take(500);

        if (fromUtc is not null)
        {
            query = query.Where(s => s.OpenedAtUtc >= fromUtc.Value);
        }

        if (toUtc is not null)
        {
            query = query.Where(s => s.OpenedAtUtc <= toUtc.Value);
        }

        var shifts = (await query.ToListAsync(ct))
            .OrderByDescending(s => s.OpenedAtUtc)
            .Take(200)
            .ToList();
        var shiftIds = shifts.Select(s => s.Id).ToArray();

        var punches = await db.Punches
            .AsNoTracking()
            .Where(p => p.UserId == userId && shiftIds.Contains(p.ShiftId))
            .OrderBy(p => p.UnixTimeSeconds)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

        return shifts
            .Select(s => ShiftDto.From(s, punches.Where(p => p.ShiftId == s.Id).Select(PunchDto.From).ToArray()))
            .ToArray();
    }

    private async Task<ShiftDto?> TryGetIdempotentResultAsync(string userId, string? idempotencyKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        var key = await db.IdempotencyKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.UserId == userId && k.Key == idempotencyKey, ct);

        if (key is null)
        {
            return null;
        }

        var punch = await db.Punches
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == key.PunchId && p.UserId == userId, ct);

        if (punch is null)
        {
            return null;
        }

        var shift = await db.Shifts.AsNoTracking().FirstAsync(s => s.Id == punch.ShiftId, ct);
        var punches = await db.Punches
            .AsNoTracking()
            .Where(p => p.ShiftId == shift.Id && p.UserId == userId)
            .OrderBy(p => p.UnixTimeSeconds)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

        return ShiftDto.From(shift, punches.Select(PunchDto.From).ToArray());
    }
}

public sealed record PunchDto(
    Guid Id,
    PunchType Type,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset OccurredAtZurich,
    long UnixTimeSeconds,
    string Timezone,
    string UtcOffset
)
{
    public static PunchDto From(Punch p) => new(
        p.Id,
        p.Type,
        p.OccurredAtUtc,
        p.OccurredAtZurich,
        p.UnixTimeSeconds,
        p.Timezone,
        p.UtcOffset
    );
}

public sealed record ShiftDto(
    Guid Id,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset OpenedAtZurich,
    DateTimeOffset? ClosedAtUtc,
    DateTimeOffset? ClosedAtZurich,
    IReadOnlyList<PunchDto> Punches
)
{
    public static ShiftDto From(Shift s, IReadOnlyList<PunchDto> punches) => new(
        s.Id,
        s.OpenedAtUtc,
        s.OpenedAtZurich,
        s.ClosedAtUtc,
        s.ClosedAtZurich,
        punches
    );
}

public sealed record StatusDto(OpenShiftDto? OpenShift);

public sealed record OpenShiftDto(Guid ShiftId, DateTimeOffset OpenedAtUtc, DateTimeOffset OpenedAtZurich);

