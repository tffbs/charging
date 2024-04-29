using WebApi.Model;

namespace WebApi.Services
{
    public class CalculatorService
    {
        public CalculatorService()
        {
            
        }

        public TariffTimeSpan GetDirectChargingTimeSpan()
        {


            return new TariffTimeSpan(DateTime.Now, DateTime.Now.AddHours(1), 0);
        }
    }
}
