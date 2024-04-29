namespace WebApi.Model
{
    public record Tariff
    {
        public string StartTime { get; init; }
        public string EndTime { get; init; } = "";
        public decimal EnergyPrice { get; init; }
    }
}
