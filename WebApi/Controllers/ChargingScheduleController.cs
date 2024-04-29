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
        private readonly ScheduleService _service;

        public ChargingScheduleController(ScheduleService calculator)
        {
            _service = calculator;
        }

        [HttpPost]
        public IEnumerable<ChargingTimeSpan> GetCharingSchedule([FromBody] Request request)
        {
            var schedule = new List<ChargingTimeSpan>();

            if(request.UserSettings.Tariffs is null || request.UserSettings.Tariffs.Count == 0)
            {
                return schedule;
            }

            var currentStartingDateTime = request.StartingTime;
            var currentStartingTime = TimeOnly.FromDateTime(request.StartingTime);
            var leavingTime = TimeOnly.Parse(request.UserSettings.LeavingTime);
            var timeLeftUntilLeavingTime = leavingTime != currentStartingTime ? leavingTime - currentStartingTime : TimeSpan.FromHours(24);
            var leavingDateTime = request.StartingTime.Add(timeLeftUntilLeavingTime);

            // conversion to percentage, as user settings contains percentage, while car data has kWh
            var currentChargePercentage = request.CarData.CurrentBatteryLevel / request.CarData.BatteryCapacity * 100;

            // if we are on the desired state of charge, return
            if (currentChargePercentage >= request.UserSettings.DesiredStateOfCharge)
            {
                schedule.Add(new ChargingTimeSpan(currentStartingDateTime, leavingDateTime, false));
                return schedule;
            }

            // check if we need to direct charge
            if (currentChargePercentage < request.UserSettings.DirectChargingPercentage)
            {
                // we need the kWh amount to know how long we need to charge
                var directAmount = (decimal)request.UserSettings.DirectChargingPercentage / 100 * request.CarData.BatteryCapacity;

                // calculating time needed based on missing charge and charge power -> kWh / kW = h
                var requiredTimeToReachDirectAmount = (directAmount - request.CarData.CurrentBatteryLevel) / request.CarData.ChargePower;
                // converting hours in decimal to DateTime -> might be some loss of accuracy due to decimal to double casting 
                var endTime = request.StartingTime.AddHours((double)requiredTimeToReachDirectAmount);

                // adjust starting time and current percentage
                currentStartingDateTime = endTime;
                currentChargePercentage = request.UserSettings.DirectChargingPercentage;

                // adding the time span to the response
                schedule.Add(new ChargingTimeSpan(request.StartingTime, endTime, true));
            }

            var tariffs = request.UserSettings.Tariffs;

            // calculating remaining charging time 
            var remainingCharge = (request.UserSettings.DesiredStateOfCharge - currentChargePercentage) / 100 * request.CarData.BatteryCapacity / request.CarData.ChargePower;
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

            var possibleTimeSpans = _service.CreatePossibleTimeSpans(tariffs, currentStartingDateTime, leavingDateTime);

            // order the tariff prices to efficiently select the cheapest one
            var tariffPrices = request.UserSettings.Tariffs.Select(t => t.EnergyPrice).Distinct().Order().ToArray();

            possibleTimeSpans.SetChargingTariffSpans(tariffPrices, remainingChargingTime);

            schedule.AddRange(possibleTimeSpans.Select(t => new ChargingTimeSpan(t.StartTime, t.EndTime, t.IsCharging)));

            return schedule.MergeTimeSpans();
        }
    }
}
