using Microsoft.AspNetCore.Mvc;
using WebApi.Entities;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChargingScheduleController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<ChargingTimeSpan> GetCharingSchedule([FromBody]Request request)
        {
            // placing the logic here temporarily, will move it to a calculator

            var schedule = new List<ChargingTimeSpan>();
            var carData = request.CarData;
            var userSettings = request.UserSettings;
            var currentStartingTime = request.StartingTime;
            var leavingTime = TimeSpan.Parse(userSettings.LeavingTime);
            var leavingDateTime = request.StartingTime.Add(leavingTime);

            // conversion to percentage, as user settings contains percentage, while car data has kWh
            var currentChargePercentage = carData.CurrentyBatteryLevel / carData.BatteryCapacity;

            // check if we need to direct charge
            if (currentChargePercentage < userSettings.DirectChargingPercentage)
            {
                // we need the kWh amount to know how long we need to charge
                var directAmount = userSettings.DirectChargingPercentage / 100 * carData.BatteryCapacity;

                // calculating time needed based on missing charge and charge power -> kWh / h = kW
                var requiredTimeToReachDirectAmount = (directAmount - carData.CurrentyBatteryLevel) / carData.ChargePower;
                // converting hours in decimal to DateTime -> might be some loss of accuracy due to decimal to double casting 
                var endTime = request.StartingTime.AddHours((double)requiredTimeToReachDirectAmount);

                // adjust starting time, we'll only need this info, the charge amount is irrelevant because of the check earlier
                currentStartingTime = endTime;

                // adding the time span to the response
                schedule.Add(new ChargingTimeSpan(request.StartingTime, endTime, true));
            }

            // creating possible timespans based on the tariffs
            // this is so that we can handle changing days
            var tariffs = request.UserSettings.Tariffs;
            var possibleTimeSpans = new List<TariffTimeSpan>();

            while (DateTime.Compare(currentStartingTime, leavingDateTime) < 0)
            {
                // converting to timespan, as 
                var currentTime = TimeOnly.FromDateTime(currentStartingTime);

                // this foreach could be eliminated by sorting tariffs and using 
                foreach (var t in tariffs)
                {
                    var start = TimeOnly.Parse(t.StartTime);
                    var end = TimeOnly.Parse(t.EndTime);

                    // IsBetween will return false if start is 23:00 and end is 2:00
                    if (currentTime.IsBetween(start, end))
                    {
                        var endDateTime = currentStartingTime.Add(end - currentTime);
                        var tariffEndDate = DateTime.Compare(endDateTime, leavingDateTime) <= 0  ? endDateTime : leavingDateTime;
                        possibleTimeSpans.Add(new TariffTimeSpan(currentStartingTime, tariffEndDate , t.EnergyPrice));

                        currentStartingTime = tariffEndDate;
                    }
                }
            }

            return schedule;
        }
    }
}
