using Microsoft.AspNetCore.Mvc;
using WebApi.Entities;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class ChargingScheduleController : ControllerBase
    {

        public ChargingScheduleController()
        {

        }
        [HttpPost]
        public IEnumerable<ChargingTimeSpan> GetCharingSchedule([FromBody] Request request)
        {
            // placing the logic here temporarily, will move it to a calculator

            var schedule = new List<ChargingTimeSpan>();
            var carData = request.CarData;
            var userSettings = request.UserSettings;
            var currentStartingTime = request.StartingTime;
            var leavingTime = TimeSpan.Parse(userSettings.LeavingTime);
            var leavingDateTime = request.StartingTime.Add(leavingTime);

            // conversion to percentage, as user settings contains percentage, while car data has kWh
            var currentChargePercentage = carData.CurrentBatteryLevel / carData.BatteryCapacity * 100;

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
                currentStartingTime = endTime;
                currentChargePercentage = userSettings.DirectChargingPercentage;

                // adding the time span to the response
                schedule.Add(new ChargingTimeSpan(request.StartingTime, endTime, true));
            }

            // creating possible timespans based on the tariffs
            // this is so that we can handle changing days
            var tariffs = request.UserSettings.Tariffs;
            var possibleTimeSpans = new List<TariffTimeSpan>();

            // WARNING while -> check if tariffs cover the whole day!!
            // if they don't this may result in an infinite loop
            while (DateTime.Compare(currentStartingTime, leavingDateTime) < 0)
            {
                // converting to timespan, as 
                var currentTime = TimeOnly.FromDateTime(currentStartingTime);

                foreach (var t in tariffs)
                {
                    var start = TimeOnly.Parse(t.StartTime);
                    var end = TimeOnly.Parse(t.EndTime);

                    // IsBetween will return false if start is 23:00 and end is 2:00
                    // possible solution: use DateTime for cases when start is greater than end
                    if (currentTime.Equals(start) || currentTime.IsBetween(start, end))
                    {
                        var endDateTime = currentStartingTime.Add(end - currentTime);
                        var tariffEndDate = DateTime.Compare(endDateTime, leavingDateTime) <= 0 ? endDateTime : leavingDateTime;
                        possibleTimeSpans.Add(new TariffTimeSpan(currentStartingTime, tariffEndDate, t.EnergyPrice));

                        currentStartingTime = tariffEndDate;
                        break;
                    }
                }
            }

            // order the tariff prices to efficiently select the cheapest one
            var tariffPrices = userSettings.Tariffs.Select(t => t.EnergyPrice).Distinct().Order().ToArray();
            // calculating remaining charging time 
            var remainingCharge = (userSettings.DesiredStateOfCharge - currentChargePercentage) / 100 * carData.BatteryCapacity / carData.ChargePower;
            var remainingChargingTime = TimeSpan.FromHours((double)remainingCharge);

            // going from the lowest price, set the timespans to charging
            // as the remaining time gets shorter we go up in price
            // this way we'll find the cheapest charge
            for (var i = 0; i < tariffPrices.Count(); i++)
            {
                foreach (var spanWithTariff in possibleTimeSpans)
                {
                    // check if we are charging in the particular timespan and if the price is low
                    if (spanWithTariff.IsCharging is false && spanWithTariff.EnergyPrice == tariffPrices[i])
                    {
                        spanWithTariff.IsCharging = true;
                        var spanWithTariffElapsedTime = spanWithTariff.EndTime - spanWithTariff.StartTime;

                        // if there is still remaining charging time stay in the loop
                        if (spanWithTariffElapsedTime <= remainingChargingTime)
                        {
                            remainingChargingTime -= spanWithTariffElapsedTime;
                            continue;
                        }

                        // if there isn't exit the loop
                        if (remainingChargingTime <= TimeSpan.Zero) break;

                        // if we would exceed the desired capacity break up the span to charging and non-charging timespans
                        var endTimeForCharging = spanWithTariff.StartTime.Add(remainingChargingTime);

                        var chargingTimeSpan = new TariffTimeSpan(spanWithTariff.StartTime, endTimeForCharging, spanWithTariff.EnergyPrice) { IsCharging = true };
                        var notChargingTimeSpan = new TariffTimeSpan(endTimeForCharging, spanWithTariff.EndTime, spanWithTariff.EnergyPrice);

                        possibleTimeSpans.Insert(possibleTimeSpans.IndexOf(spanWithTariff) + 1, chargingTimeSpan);
                        possibleTimeSpans.Insert(possibleTimeSpans.IndexOf(spanWithTariff) + 2, notChargingTimeSpan);
                        possibleTimeSpans.Remove(spanWithTariff);
                        remainingChargingTime = TimeSpan.Zero;
                        break;
                    }
                }

                if (remainingChargingTime <= TimeSpan.Zero) break;
            }

            schedule.AddRange(possibleTimeSpans.Select(t => new ChargingTimeSpan(t.StartTime, t.EndTime, t.IsCharging)));

            return schedule;
        }
    }
}
