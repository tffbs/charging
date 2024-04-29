using WebApi.Model;

namespace WebApi.ExtensionMethods
{
    public static class ChargingScheduleExtensionMethods
    {
        public static List<ChargingTimeSpan> MergeTimeSpans(this List<ChargingTimeSpan> schedule)
        {
            var newSchedule = new List<ChargingTimeSpan>();
            var currentSpan = schedule.First();

            for (var i = 1; i < schedule.Count(); i++)
            {
                if(currentSpan.IsCharging == schedule[i].IsCharging)
                {
                    currentSpan.EndTime = schedule[i].EndTime;
                }
                else
                {
                    newSchedule.Add(currentSpan);
                    currentSpan = schedule[i];
                }

                if(i == schedule.Count() - 1) newSchedule.Add(currentSpan);
            }

            return newSchedule;
        }

        public static void SetChargingTariffSpans(this List<TariffTimeSpan> possibleTimeSpans, decimal[] tariffPrices, TimeSpan remainingChargingTime)
        {

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
        }
    }
}
