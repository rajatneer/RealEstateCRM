using Microsoft.Extensions.Logging.Abstractions;
using RealEstateCRM.Models.DTOs;
using RealEstateCRM.Services;

namespace RealEstateCRM.Tests;

public class CalculatorServiceTests
{
    private readonly CalculatorService _sut = new(NullLogger<CalculatorService>.Instance);

    [Fact]
    public void Emi_matches_known_value()
    {
        // 10,00,000 at 12% p.a. over 12 months -> EMI ~ 88,848.79
        var result = _sut.CalculateEmi(new EmiCalculatorRequest { LoanAmount = 1_000_000m, AnnualInterestRate = 12m, TenureMonths = 12 });

        Assert.InRange(result.MonthlyEmi, 88_848.00m, 88_849.50m);
        Assert.Equal(Math.Round(result.MonthlyEmi * 12, 2), Math.Round(result.TotalPayment, 2), 1);
        Assert.True(result.TotalInterest > 0);
    }

    [Fact]
    public void Emi_with_zero_interest_is_straight_division()
    {
        var result = _sut.CalculateEmi(new EmiCalculatorRequest { LoanAmount = 120_000m, AnnualInterestRate = 0m, TenureMonths = 12 });

        Assert.Equal(10_000m, result.MonthlyEmi);
        Assert.Equal(0m, result.TotalInterest);
    }

    [Fact]
    public void Stamp_duty_uses_state_rates()
    {
        var result = _sut.CalculateStampDuty(new StampDutyRequest { PropertyValue = 1_000_000m, State = "Maharashtra" });

        Assert.Equal(50_000m, result.StampDutyAmount);
        Assert.Equal(10_000m, result.RegistrationAmount);
        Assert.Equal(60_000m, result.TotalCost);
    }

    [Fact]
    public void Stamp_duty_state_lookup_is_case_insensitive()
    {
        var result = _sut.CalculateStampDuty(new StampDutyRequest { PropertyValue = 100_000m, State = "kArNaTaKa" });
        Assert.Equal(5.0m, result.StampDutyPercent);
    }

    [Fact]
    public void Unknown_state_falls_back_to_default_rates()
    {
        var result = _sut.CalculateStampDuty(new StampDutyRequest { PropertyValue = 100_000m, State = "Atlantis" });

        Assert.Equal(5.0m, result.StampDutyPercent);
        Assert.Equal(1.0m, result.RegistrationPercent);
    }
}
