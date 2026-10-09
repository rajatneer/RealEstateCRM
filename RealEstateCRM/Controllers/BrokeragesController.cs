using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrokeragesController : ControllerBase
    {
        private readonly IBrokerageService _brokerageService;
        private readonly ILogger<BrokeragesController> _logger;

        public BrokeragesController(IBrokerageService brokerageService, ILogger<BrokeragesController> logger)
        {
            _brokerageService = brokerageService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] BrokeragePaymentStatus? status = null)
        {
            try
            {
                var brokerages = await _brokerageService.GetAllAsync(status);
                return Ok(brokerages.Select(x => x.ToDto()));
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error in GetAll brokerages");
                return StatusCode(500, "An error occurred while retrieving brokerages.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var brokerage = await _brokerageService.GetByIdAsync(id);
                if (brokerage == null) return NotFound();
                return Ok(brokerage.ToDto());
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error in GetById brokerage {BrokerageId}", id);
                return StatusCode(500, "An error occurred while retrieving the brokerage.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BrokerageCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var brokerage = await _brokerageService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = brokerage.Id }, brokerage.ToDto());
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error creating brokerage");
                return StatusCode(500, "An error occurred while creating the brokerage.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BrokerageUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var brokerage = await _brokerageService.UpdateAsync(id, dto);
                if (brokerage == null) return NotFound();
                return Ok(brokerage.ToDto());
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error updating brokerage {BrokerageId}", id);
                return StatusCode(500, "An error occurred while updating the brokerage.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _brokerageService.DeleteAsync(id);
                if (!result) return NotFound();
                return NoContent();
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error deleting brokerage {BrokerageId}", id);
                return StatusCode(500, "An error occurred while deleting the brokerage.");
            }
        }
    }
}
