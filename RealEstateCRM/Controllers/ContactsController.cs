using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactsController : ControllerBase
    {
        private readonly IContactService _contactService;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(IContactService contactService, ILogger<ContactsController> logger)
        {
            _contactService = contactService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var contacts = await _contactService.GetAllAsync();
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll contacts");
                return StatusCode(500, "An error occurred while retrieving contacts.");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? q = null, [FromQuery] ContactType? type = null)
        {
            try
            {
                var contacts = await _contactService.SearchAsync(q, type);
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching contacts");
                return StatusCode(500, "An error occurred while searching contacts.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var contact = await _contactService.GetByIdAsync(id);
                if (contact == null) return NotFound();
                return Ok(contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetById contact {ContactId}", id);
                return StatusCode(500, "An error occurred while retrieving the contact.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContactCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var contact = await _contactService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = contact.Id }, contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                return StatusCode(500, "An error occurred while creating the contact.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ContactUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var contact = await _contactService.UpdateAsync(id, dto);
                if (contact == null) return NotFound();
                return Ok(contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact {ContactId}", id);
                return StatusCode(500, "An error occurred while updating the contact.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _contactService.DeleteAsync(id);
                if (!result) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact {ContactId}", id);
                return StatusCode(500, "An error occurred while deleting the contact.");
            }
        }
    }
}
