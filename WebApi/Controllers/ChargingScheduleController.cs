using Microsoft.AspNetCore.Mvc;
using WebApi.ExtensionMethods;
using WebApi.Model;
using WebApi.Services;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class ChargingScheduleController : ControllerBase
    {
        private readonly ScheduleService _calculator;

        public ChargingScheduleController(ScheduleService calculator)
        {
            _calculator = calculator;
        }

        [HttpPost]
        public IEnumerable<ChargingTimeSpan> GetCharingSchedule([FromBody] Request request)
        {
            var schedule = new List<ChargingTimeSpan>();

            if(request.UserSettings.Tariffs is null || request.UserSettings.Tariffs.Count == 0)
            {
                return schedule;
            }

            var carData = request.CarData;
            var userSettings = request.UserSettings;
            var currentStartingDateTime = request.StartingTime;
            var currentStartingTime = TimeOnly.FromDateTime(request.StartingTime);
            var leavingTime = TimeOnly.Parse(userSettings.LeavingTime);
            var timeLeftUntilLeavingTime = leavingTime != currentStartingTime ? leavingTime - currentStartingTime : TimeSpan.FromHours(24);
            var leavingDateTime = request.StartingTime.Add(timeLeftUntilLeavingTime);

            // conversion to percentage, as user settings contains percentage, while car data has kWh
            var currentChargePercentage = carData.CurrentBatteryLevel / carData.BatteryCapacity * 100;

            // if we are on the desired state of charge, return
            if (currentChargePercentage >= userSettings.DesiredStateOfCharge)
            {
                schedule.Add(new ChargingTimeSpan(currentStartingDateTime, leavingDateTime, false));
                return schedule;
            }

            // check if we need to direct charge
            if (currentChargePercentage < userSettings.DirectChargingPercentage)
            {
                // we need the kWh amount to know how long we need to charge
                var directAmount = (decimal)userSettings.DirectChargingPercentage / 100 * carData.BatteryCapacity;

                // calculating time needed based on missing charge and charge power -> kWh / kW = h
                var requiredTimeToReachDirectAmount = (directAmount - carData.CurrentBatteryLevel) / carData.ChargePower;
                // converting hours in decimal to DateTime -> might be some loss of accuracy due to decimal to double casting 
                var endTime = request.StartingTime.AddHours((double)requiredTimeToReachDirectAmount);

                // adjust starting time and current percentage
                currentStartingDateTime = endTime;
                currentChargePercentage = userSettings.DirectChargingPercentage;

                // adding the time span to the response
                schedule.Add(new ChargingTimeSpan(request.StartingTime, endTime, true));
            }

            var tariffs = request.UserSettings.Tariffs;
            var possibleTimeSpans = new List<TariffTimeSpan>();

            // calculating remaining charging time 
            var remainingCharge = (userSettings.DesiredStateOfCharge - currentChargePercentage) / 100 * carData.BatteryCapacity / carData.ChargePower;
            var remainingChargingTime = TimeSpan.FromHours((double)remainingCharge);

            if (tariffs.Count == 1)
            {
                var endTimeForCharging = currentStartingDateTime.Add(remainingChargingTime);

                var chargingTimeSpan = new ChargingTimeSpan(currentStartingDateTime, endTimeForCharging, true);
                var notChargingTimeSpan = new ChargingTimeSpan(endTimeForCharging, leavingDateTime, false);

                schedule.Add(chargingTimeSpan);
                schedule.Add(notChargingTimeSpan);

                return schedule.MergeTimeSpans();
            }

            while (DateTime.Compare(currentStartingDateTime, leavingDateTime) < 0)
            {
                var currentTime = TimeOnly.FromDateTime(currentStartingDateTime);

                foreach (var t in tariffs)
                {
                    var start = TimeOnly.Parse(t.StartTime);
                    var end = TimeOnly.Parse(t.EndTime);

                    if (currentTime.Equals(start) || currentTime.IsBetween(start, end))
                    {
                        var endDateTime = currentStartingDateTime.Add(end - currentTime);
                        var tariffEndDate = DateTime.Compare(endDateTime, leavingDateTime) <= 0 ? endDateTime : leavingDateTime;
                        possibleTimeSpans.Add(new TariffTimeSpan(currentStartingDateTime, tariffEndDate, t.EnergyPrice));

                        currentStartingDateTime = tariffEndDate;
                        break;
                    }
                }
            }

            // order the tariff prices to efficiently select the cheapest one
            var tariffPrices = userSettings.Tariffs.Select(t => t.EnergyPrice).Distinct().Order().ToArray();

            possibleTimeSpans.SetChargingTariffSpans(tariffPrices, remainingChargingTime);

            schedule.AddRange(possibleTimeSpans.Select(t => new ChargingTimeSpan(t.StartTime, t.EndTime, t.IsCharging)));

            return schedule.MergeTimeSpans();
        }
    }
}
