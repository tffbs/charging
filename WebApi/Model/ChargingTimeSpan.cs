namespace WebApi.Model
{
    public record ChargingTimeSpan
    {
        public ChargingTimeSpan(DateTime startTime, DateTime endTime, bool isCharging)
        {
            StartTime = startTime;
            EndTime = endTime;
            IsCharging = isCharging;
        }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsCharging { get; set; }
    }
}
