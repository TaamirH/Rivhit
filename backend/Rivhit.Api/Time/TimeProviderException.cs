namespace Rivhit.Api.Time;

public sealed class TimeProviderException(string message, Exception? innerException = null)
    : Exception(message, innerException);

