using CarePoint.Application.Configuration;
using CarePoint.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace CarePoint.Infrastructure.Services;

public sealed class ClinicClock : IClinicClock
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public ClinicClock(TimeProvider timeProvider, IOptions<ClinicTimeSettings> settings)
    {
        _timeProvider = timeProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.TimeZoneId);
    }

    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;
    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, _timeZone);
}
