using RealEstateCRM.Models.DTOs;

namespace RealEstateCRM.Services
{
    public interface ICalculatorService
    {
        EmiCalculatorResult CalculateEmi(EmiCalculatorRequest request);
        StampDutyResult CalculateStampDuty(StampDutyRequest request);
    }

    public class CalculatorService : ICalculatorService
    {
        private readonly ILogger<CalculatorService> _logger;

        // State-wise stamp duty and registration rates for India
        private static readonly Dictionary<string, (decimal StampDuty, decimal Registration)> StateRates = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Maharashtra"] = (5.0m, 1.0m),
            ["Karnataka"] = (5.0m, 1.0m),
            ["Tamil Nadu"] = (7.0m, 1.0m),
            ["Telangana"] = (5.0m, 0.5m),
            ["Andhra Pradesh"] = (5.0m, 0.5m),
            ["Delhi"] = (6.0m, 1.0m),
            ["Uttar Pradesh"] = (7.0m, 1.0m),
            ["Rajasthan"] = (5.0m, 1.0m),
            ["Gujarat"] = (4.9m, 1.0m),
            ["West Bengal"] = (6.0m, 1.0m),
            ["Madhya Pradesh"] = (7.5m, 3.0m),
            ["Kerala"] = (8.0m, 2.0m),
            ["Punjab"] = (7.0m, 1.0m),
            ["Haryana"] = (7.0m, 1.0m),
            ["Bihar"] = (6.0m, 2.0m),
            ["Odisha"] = (5.0m, 1.0m),
            ["Jharkhand"] = (4.0m, 3.0m),
            ["Assam"] = (8.25m, 0.0m),
            ["Chhattisgarh"] = (5.0m, 4.0m),
            ["Goa"] = (3.5m, 1.0m),
            ["Himachal Pradesh"] = (5.0m, 2.0m),
            ["Uttarakhand"] = (5.0m, 2.0m),
        };

        public CalculatorService(ILogger<CalculatorService> logger)
        {
            _logger = logger;
        }

        public EmiCalculatorResult CalculateEmi(EmiCalculatorRequest request)
        {
            try
            {
                var principal = request.LoanAmount;
                var monthlyRate = request.AnnualInterestRate / 12 / 100;
                var months = request.TenureMonths;

                decimal emi;
                if (monthlyRate == 0)
                {
                    emi = principal / months;
                }
                else
                {
                    // EMI = P * r * (1+r)^n / ((1+r)^n - 1)
                    var power = (decimal)Math.Pow((double)(1 + monthlyRate), months);
                    emi = principal * monthlyRate * power / (power - 1);
                }

                var totalPayment = emi * months;
                var totalInterest = totalPayment - principal;

                return new EmiCalculatorResult
                {
                    LoanAmount = principal,
                    AnnualInterestRate = request.AnnualInterestRate,
                    TenureMonths = months,
                    MonthlyEmi = Math.Round(emi, 2),
                    TotalInterest = Math.Round(totalInterest, 2),
                    TotalPayment = Math.Round(totalPayment, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating EMI for amount {Amount}", request.LoanAmount);
                throw;
            }
        }

        public StampDutyResult CalculateStampDuty(StampDutyRequest request)
        {
            try
            {
                decimal stampDutyPercent;
                decimal registrationPercent;

                if (StateRates.TryGetValue(request.State, out var rates))
                {
                    stampDutyPercent = rates.StampDuty;
                    registrationPercent = rates.Registration;
                }
                else
                {
                    // Default rates if state not found
                    stampDutyPercent = 5.0m;
                    registrationPercent = 1.0m;
                    _logger.LogWarning("State '{State}' not found in stamp duty table, using default rates", request.State);
                }

                var stampDutyAmount = request.PropertyValue * stampDutyPercent / 100m;
                var registrationAmount = request.PropertyValue * registrationPercent / 100m;
                var totalCost = stampDutyAmount + registrationAmount;

                return new StampDutyResult
                {
                    PropertyValue = request.PropertyValue,
                    State = request.State,
                    StampDutyPercent = stampDutyPercent,
                    StampDutyAmount = Math.Round(stampDutyAmount, 2),
                    RegistrationPercent = registrationPercent,
                    RegistrationAmount = Math.Round(registrationAmount, 2),
                    TotalCost = Math.Round(totalCost, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating stamp duty for state {State}", request.State);
                throw;
            }
        }
    }
}
