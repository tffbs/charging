namespace WebApi.Entities
{
    public record Request
    {
        public DateTime StartingTime { get; init; }
        public UserSettings UserSettings { get; init; } = new UserSettings();
        public CarData CarData { get; init; } = new CarData();
    }

    // <>
    public record UserSettings
    {
        /// <summary>
        /// The desired battery percentage of the battery of the car at Leaving Time, Example: 90
        /// </summary>
        public int DesiredStateOfCharge { get; init; }

        /// <summary>
        /// This is the leaving time of the user, for simplicity we assume it is the same for all days of the week, Example: 07:00
        /// </summary>
        public string LeavingTime { get; init; } //= "01:00";

        /// <summary>
        /// This is the minimum percentage of the battery we will always charge directly. Example: 20. If the car battery level would be let's say 15%, always first charge up to 20% before doing anything else
        /// </summary>
        public int DirectChargingPercentage { get; init; }

        /// <summary>
        /// Energy prices vary during the day, as shown below. For simplicity you can assume that the pricesare the same for every day of the week.
        /// </summary>
        public List<Tariff> Tariffs { get; init; } = new List<Tariff>();
    }

    public record Tariff
    {
        public string StartTime { get; init; }
        public string EndTime { get; init; }
        public decimal EnergyPrice { get; init; }
    }

    public record CarData
    {
        /// <summary>
        /// The power in kW (kilowatt) used when when charging. You can assume 100% efficiency and that it is constant over time. Example: 9.6
        /// </summary>
        public decimal ChargePower { get; init; }

        /// <summary>
        /// The amount of energy in kWh (kilowatt hour) that the battery can store. Example: 55
        /// </summary>
        public decimal BatteryCapacity { get; init; }

        /// <summary>
        /// The level of charge in kWh the car battery at any one time, Example: 12
        /// </summary>
        public decimal CurrentBatteryLevel { get; init; }
    }

    public record ChargingTimeSpan(DateTime StartTime, DateTime EndTime, bool IsCharging);

    public record TariffTimeSpan(DateTime StartTime, DateTime EndTime, decimal EnergyPrice)
    {
        public bool IsCharging { get; set; } = false;
    }
}
