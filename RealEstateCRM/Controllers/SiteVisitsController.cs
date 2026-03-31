using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SiteVisitsController : ControllerBase
    {
        private readonly ISiteVisitService _siteVisitService;
        private readonly ILogger<SiteVisitsController> _logger;

        public SiteVisitsController(ISiteVisitService siteVisitService, ILogger<SiteVisitsController> logger)
        {
            _siteVisitService = siteVisitService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] SiteVisitStatus? status = null)
        {
            try
            {
                var visits = await _siteVisitService.GetAllAsync(status);
                return Ok(visits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll site visits");
                return StatusCode(500, "An error occurred while retrieving site visits.");
            }
        }

        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcoming()
        {
            try
            {
                var visits = await _siteVisitService.GetUpcomingAsync();
                return Ok(visits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming site visits");
                return StatusCode(500, "An error occurred while retrieving upcoming site visits.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var visit = await _siteVisitService.GetByIdAsync(id);
                if (visit == null) return NotFound();
                return Ok(visit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetById site visit {SiteVisitId}", id);
                return StatusCode(500, "An error occurred while retrieving the site visit.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SiteVisitCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var visit = await _siteVisitService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = visit.Id }, visit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site visit");
                return StatusCode(500, "An error occurred while creating the site visit.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SiteVisitUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var visit = await _siteVisitService.UpdateAsync(id, dto);
                if (visit == null) return NotFound();
                return Ok(visit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site visit {SiteVisitId}", id);
                return StatusCode(500, "An error occurred while updating the site visit.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _siteVisitService.DeleteAsync(id);
                if (!result) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site visit {SiteVisitId}", id);
                return StatusCode(500, "An error occurred while deleting the site visit.");
            }
        }
    }
}
