namespace WebApi.Model
{
    public record TariffTimeSpan(DateTime StartTime, DateTime EndTime, decimal EnergyPrice)
    {
        public bool IsCharging { get; set; } = false;
    }
}
