using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Models.DTOs;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InteractionsController : ControllerBase
    {
        private readonly IInteractionService _interactionService;
        private readonly ILogger<InteractionsController> _logger;

        public InteractionsController(IInteractionService interactionService, ILogger<InteractionsController> logger)
        {
            _interactionService = interactionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? contactId = null, [FromQuery] int? propertyId = null)
        {
            try
            {
                var interactions = await _interactionService.GetAllAsync(contactId, propertyId);
                return Ok(interactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll interactions");
                return StatusCode(500, "An error occurred while retrieving interactions.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var interaction = await _interactionService.GetByIdAsync(id);
                if (interaction == null) return NotFound();
                return Ok(interaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetById interaction {InteractionId}", id);
                return StatusCode(500, "An error occurred while retrieving the interaction.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InteractionCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var interaction = await _interactionService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = interaction.Id }, interaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating interaction");
                return StatusCode(500, "An error occurred while creating the interaction.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _interactionService.DeleteAsync(id);
                if (!result) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting interaction {InteractionId}", id);
                return StatusCode(500, "An error occurred while deleting the interaction.");
            }
        }
    }
}
