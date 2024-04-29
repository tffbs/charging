namespace WebApi.Model
{
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
}
