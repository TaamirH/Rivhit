using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rivhit.Api.Attendance;
using Rivhit.Api.Data;
using Rivhit.Api.Time;

namespace Rivhit.Api.Tests;

public sealed class ClockServiceTests
{
    [Fact]
    public async Task ClockIn_then_ClockOut_closes_shift()
    {
        await using var testDb = await CreateDbAsync();
        var db = testDb.Db;
        var time = new FakeTimeProvider();
        var service = new ClockService(db, time);

        var userId = "u1";

        var shift = await service.ClockInAsync(userId, idempotencyKey: null, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, shift.Id);
        Assert.Null(shift.ClosedAtUtc);

        var statusOpen = await service.GetStatusAsync(userId, CancellationToken.None);
        Assert.NotNull(statusOpen.OpenShift);

        var closed = await service.ClockOutAsync(userId, idempotencyKey: null, CancellationToken.None);
        Assert.NotNull(closed.ClosedAtUtc);
        Assert.Equal(2, closed.Punches.Count);

        var statusClosed = await service.GetStatusAsync(userId, CancellationToken.None);
        Assert.Null(statusClosed.OpenShift);
    }

    [Fact]
    public async Task ClockIn_twice_throws()
    {
        await using var testDb = await CreateDbAsync();
        var db = testDb.Db;
        var time = new FakeTimeProvider();
        var service = new ClockService(db, time);

        var userId = "u1";
        await service.ClockInAsync(userId, idempotencyKey: null, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ClockInAsync(userId, idempotencyKey: null, CancellationToken.None));
    }

    [Fact]
    public async Task ClockIn_is_idempotent_per_user_key()
    {
        await using var testDb = await CreateDbAsync();
        var db = testDb.Db;
        var time = new FakeTimeProvider();
        var service = new ClockService(db, time);

        var userId = "u1";
        var key = Guid.NewGuid().ToString("N");

        var a = await service.ClockInAsync(userId, key, CancellationToken.None);
        var b = await service.ClockInAsync(userId, key, CancellationToken.None);

        Assert.Equal(a.Id, b.Id);
        Assert.Single(a.Punches);
        Assert.Single(b.Punches);
    }

    private static async Task<TestDb> CreateDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return new TestDb(db, connection);
    }

    private sealed class TestDb(AppDbContext db, SqliteConnection connection) : IAsyncDisposable
    {
        public AppDbContext Db { get; } = db;

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}

file sealed class FakeTimeProvider : ITimeProvider
{
    private long _i;

    public Task<ZurichTimeSnapshot> GetZurichNowAsync(CancellationToken cancellationToken)
    {
        var utc = DateTimeOffset.Parse("2026-02-23T08:00:00Z").AddSeconds(_i++);
        var offset = TimeSpan.FromHours(1);
        var zurich = utc.ToOffset(offset);

        return Task.FromResult(new ZurichTimeSnapshot(
            UtcDateTime: utc,
            ZurichDateTime: zurich,
            UnixTimeSeconds: utc.ToUnixTimeSeconds(),
            Timezone: "Europe/Zurich",
            UtcOffset: "+01:00",
            RawResponse: "{}"
        ));
    }
}