using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Rivhit.Api.Data;

namespace Rivhit.Api.Reports;

public sealed class AdminReportService(AppDbContext db)
{
    public async Task<string> ExportShiftsCsvAsync(DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken ct)
    {
        var shiftsQuery = db.Shifts.AsNoTracking();

        if (fromUtc is not null)
        {
            shiftsQuery = shiftsQuery.Where(s => s.OpenedAtUtc >= fromUtc.Value);
        }

        if (toUtc is not null)
        {
            shiftsQuery = shiftsQuery.Where(s => s.OpenedAtUtc <= toUtc.Value);
        }

        var query =
            from s in shiftsQuery
            join u in db.Users.AsNoTracking() on s.UserId equals u.Id
            orderby s.OpenedAtUtc descending
            select new
            {
                s.Id,
                s.UserId,
                Email = u.Email ?? "",
                s.OpenedAtUtc,
                s.OpenedAtZurich,
                s.ClosedAtUtc,
                s.ClosedAtZurich
            };

        var rows = await query.Take(2000).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("shift_id,user_id,email,opened_at_utc,opened_at_zurich,closed_at_utc,closed_at_zurich,duration_seconds");

        foreach (var r in rows)
        {
            var durationSeconds = r.ClosedAtUtc is null
                ? ""
                : Math.Max(0, (r.ClosedAtUtc.Value - r.OpenedAtUtc).TotalSeconds).ToString("0", CultureInfo.InvariantCulture);

            sb.AppendLine(string.Join(",",
                Csv(r.Id.ToString()),
                Csv(r.UserId),
                Csv(r.Email),
                Csv(r.OpenedAtUtc.ToString("O")),
                Csv(r.OpenedAtZurich.ToString("O")),
                Csv(r.ClosedAtUtc?.ToString("O") ?? ""),
                Csv(r.ClosedAtZurich?.ToString("O") ?? ""),
                Csv(durationSeconds)
            ));
        }

        return sb.ToString();
    }

    private static string Csv(string value)
    {
        // Minimal CSV escaping (enough for emails/ids/timestamps).
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

