namespace Rivhit.Api.Time;

public interface ITimeProvider
{
    Task<ZurichTimeSnapshot> GetZurichNowAsync(CancellationToken cancellationToken);
}

