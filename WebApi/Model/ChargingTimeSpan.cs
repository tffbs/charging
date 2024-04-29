namespace WebApi.Model
{
    public record ChargingTimeSpan(DateTime StartTime, DateTime EndTime, bool IsCharging);
}
