using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadsController : ControllerBase
    {
        private readonly ILeadService _leadService;
        private readonly ILogger<LeadsController> _logger;

        public LeadsController(ILeadService leadService, ILogger<LeadsController> logger)
        {
            _leadService = leadService;
            _logger = logger;
        }

        [HttpGet("{id}/timeline")]
        public async Task<IActionResult> GetTimeline(int id)
        {
            try
            {
                var timeline = await _leadService.GetTimelineAsync(id);
                if (timeline == null) return NotFound();
                return Ok(timeline);
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error getting timeline for lead {LeadId}", id);
                return StatusCode(500, "An error occurred while retrieving the timeline.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] LeadStage? stage = null)
        {
            try
            {
                var leads = await _leadService.GetAllAsync(stage);
                return Ok(leads.Select(x => x.ToDto()));
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error in GetAll leads");
                return StatusCode(500, "An error occurred while retrieving leads.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var lead = await _leadService.GetByIdAsync(id);
                if (lead == null) return NotFound();
                return Ok(lead.ToDto());
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error in GetById lead {LeadId}", id);
                return StatusCode(500, "An error occurred while retrieving the lead.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LeadCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var lead = await _leadService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = lead.Id }, lead.ToDto());
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error creating lead");
                return StatusCode(500, "An error occurred while creating the lead.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] LeadUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var lead = await _leadService.UpdateAsync(id, dto);
                if (lead == null) return NotFound();
                return Ok(lead.ToDto());
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error updating lead {LeadId}", id);
                return StatusCode(500, "An error occurred while updating the lead.");
            }
        }

        /// <summary>Owner-only: hand a lead to an agent. The agent can then see and work on it.</summary>
        [Authorize(Roles = Roles.Owner)]
        [HttpPut("{id}/assign")]
        public async Task<IActionResult> Assign(int id, [FromBody] AssignDto dto)
        {
            var lead = await _leadService.AssignAsync(id, dto.UserId);
            if (lead == null) return NotFound();
            return Ok(lead.ToDto());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _leadService.DeleteAsync(id);
                if (!result) return NotFound();
                return NoContent();
            }
            catch (Exception ex) when (ex is not AppException)
            {
                _logger.LogError(ex, "Error deleting lead {LeadId}", id);
                return StatusCode(500, "An error occurred while deleting the lead.");
            }
        }
    }
}
