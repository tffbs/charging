namespace WebApi.Model
{
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
        public string LeavingTime { get; init; }

        /// <summary>
        /// This is the minimum percentage of the battery we will always charge directly. Example: 20. If the car battery level would be let's say 15%, always first charge up to 20% before doing anything else
        /// </summary>
        public int DirectChargingPercentage { get; init; }

        /// <summary>
        /// Energy prices vary during the day, as shown below. For simplicity you can assume that the pricesare the same for every day of the week.
        /// </summary>
        public List<Tariff> Tariffs { get; init; } = new List<Tariff>();
    }
}
