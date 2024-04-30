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

            var currentChargePercentage = request.CarData.CurrentBatteryLevel / request.CarData.BatteryCapacity * 100;

            if (currentChargePercentage >= request.UserSettings.DesiredStateOfCharge)
            {
                schedule.Add(new ChargingTimeSpan(currentStartingDateTime, leavingDateTime, false));
                return schedule;
            }

            if (currentChargePercentage < request.UserSettings.DirectChargingPercentage)
            {
                var directAmount = (decimal)request.UserSettings.DirectChargingPercentage / 100 * request.CarData.BatteryCapacity;

                var requiredTimeToReachDirectAmount = (directAmount - request.CarData.CurrentBatteryLevel) / request.CarData.ChargePower;
                var endTime = request.StartingTime.AddHours((double)requiredTimeToReachDirectAmount);

                currentStartingDateTime = endTime;
                currentChargePercentage = request.UserSettings.DirectChargingPercentage;

                schedule.Add(new ChargingTimeSpan(request.StartingTime, endTime, true));
            }

            var tariffs = request.UserSettings.Tariffs;

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

            var tariffPrices = request.UserSettings.Tariffs.Select(t => t.EnergyPrice).Distinct().Order().ToArray();

            possibleTimeSpans.SetChargingTariffSpans(tariffPrices, remainingChargingTime);

            schedule.AddRange(possibleTimeSpans.Select(t => new ChargingTimeSpan(t.StartTime, t.EndTime, t.IsCharging)));

            return schedule.MergeTimeSpans();
        }
    }
}
