using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Models.DTOs;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalculatorController : ControllerBase
    {
        private readonly ICalculatorService _calculatorService;
        private readonly ILogger<CalculatorController> _logger;

        public CalculatorController(ICalculatorService calculatorService, ILogger<CalculatorController> logger)
        {
            _calculatorService = calculatorService;
            _logger = logger;
        }

        [HttpPost("emi")]
        public IActionResult CalculateEmi([FromBody] EmiCalculatorRequest request)
        {
            try
            {
                if (request.LoanAmount <= 0 || request.AnnualInterestRate < 0 || request.TenureMonths <= 0)
                    return BadRequest("Invalid input values. Loan amount and tenure must be positive.");

                var result = _calculatorService.CalculateEmi(request);
                return Ok(result);
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error calculating EMI");
                return StatusCode(500, "An error occurred while calculating EMI.");
            }
        }

        [HttpPost("stampduty")]
        public IActionResult CalculateStampDuty([FromBody] StampDutyRequest request)
        {
            try
            {
                if (request.PropertyValue <= 0 || string.IsNullOrWhiteSpace(request.State))
                    return BadRequest("Invalid input. Property value must be positive and state is required.");

                var result = _calculatorService.CalculateStampDuty(request);
                return Ok(result);
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error calculating stamp duty");
                return StatusCode(500, "An error occurred while calculating stamp duty.");
            }
        }

        [HttpGet("states")]
        public IActionResult GetStates()
        {
            try
            {
                var states = new[]
                {
                    "Andhra Pradesh", "Assam", "Bihar", "Chhattisgarh", "Delhi",
                    "Goa", "Gujarat", "Haryana", "Himachal Pradesh", "Jharkhand",
                    "Karnataka", "Kerala", "Madhya Pradesh", "Maharashtra", "Odisha",
                    "Punjab", "Rajasthan", "Tamil Nadu", "Telangana", "Uttar Pradesh",
                    "Uttarakhand", "West Bengal"
                };
                return Ok(states);
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error retrieving states list");
                return StatusCode(500, "An error occurred.");
            }
        }
    }
}
