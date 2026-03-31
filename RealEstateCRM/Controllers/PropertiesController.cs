using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly IPropertyService _propertyService;
        private readonly ILogger<PropertiesController> _logger;

        public PropertiesController(IPropertyService propertyService, ILogger<PropertiesController> logger)
        {
            _propertyService = propertyService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PropertyStatus? status = null)
        {
            try
            {
                var properties = await _propertyService.GetAllAsync(status);
                return Ok(properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll properties");
                return StatusCode(500, "An error occurred while retrieving properties.");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? q = null,
            [FromQuery] PropertyType? type = null,
            [FromQuery] PropertyStatus? status = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int? minBeds = null)
        {
            try
            {
                var properties = await _propertyService.SearchAsync(q, type, status, minPrice, maxPrice, minBeds);
                return Ok(properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching properties");
                return StatusCode(500, "An error occurred while searching properties.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var property = await _propertyService.GetByIdAsync(id);
                if (property == null) return NotFound();
                return Ok(property);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetById property {PropertyId}", id);
                return StatusCode(500, "An error occurred while retrieving the property.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PropertyCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var property = await _propertyService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = property.Id }, property);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property");
                return StatusCode(500, "An error occurred while creating the property.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PropertyUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var property = await _propertyService.UpdateAsync(id, dto);
                if (property == null) return NotFound();
                return Ok(property);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property {PropertyId}", id);
                return StatusCode(500, "An error occurred while updating the property.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _propertyService.DeleteAsync(id);
                if (!result) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property {PropertyId}", id);
                return StatusCode(500, "An error occurred while deleting the property.");
            }
        }
    }
}
