using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Models;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    /// <summary>Master list of people for Project Manager / Escalation Owner dropdowns.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PeopleController : ControllerBase
    {
        private readonly IPersonService _people;

        public PeopleController(IPersonService people) => _people = people;

        public class PersonSaveDto
        {
            [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(100)]
            public string Name { get; set; } = string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok((await _people.GetAllAsync()).Select(p => new { p.Id, p.Name }));

        [Authorize(Roles = Roles.Owner)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PersonSaveDto dto)
        {
            var person = await _people.CreateAsync(dto.Name);
            return Ok(new { person.Id, person.Name });
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id) =>
            await _people.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
