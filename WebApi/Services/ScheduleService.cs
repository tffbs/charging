using WebApi.Model;

namespace WebApi.Services
{
    public class ScheduleService
    {
        public ScheduleService()
        {
            
        }

        public List<TariffTimeSpan> CreatePossibleTimeSpans(List<Tariff> tariffs, DateTime currentStartingDateTime, DateTime leavingDateTime)
        {
            var possibleTimeSpans = new List<TariffTimeSpan>();

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

            return possibleTimeSpans;
        }
    }
}
