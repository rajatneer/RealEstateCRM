using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    /// <summary>
    /// Villa/plot projects and their dated milestones. Everyone in the company can read;
    /// only Owners can create, change or delete.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projects;

        public ProjectsController(IProjectService projects)
        {
            _projects = projects;
        }

        private static DateTime Today => DateTime.UtcNow.Date;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProjectStatus? status = null)
        {
            var projects = await _projects.GetAllAsync(status);
            return Ok(projects.Select(p => p.ToDto(Today)));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var project = await _projects.GetByIdAsync(id);
            if (project == null) return NotFound();
            return Ok(project.ToDto(Today));
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectCreateDto dto)
        {
            var project = await _projects.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project.ToDto(Today));
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProjectUpdateDto dto)
        {
            var project = await _projects.UpdateAsync(id, dto);
            if (project == null) return NotFound();
            return Ok(project.ToDto(Today));
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await _projects.DeleteAsync(id) ? NoContent() : NotFound();
        }

        // ── Milestones ──

        /// <summary>Overdue and soon-due steps across all projects (default: next 30 days).</summary>
        [HttpGet("milestones/upcoming")]
        public async Task<IActionResult> Upcoming([FromQuery] int days = 30)
        {
            var milestones = await _projects.GetUpcomingAsync(Math.Clamp(days, 0, 3650));
            return Ok(milestones.Select(m => m.ToDto(Today, includeProjectName: true)));
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpPost("{id:int}/milestones")]
        public async Task<IActionResult> AddMilestone(int id, [FromBody] MilestoneCreateDto dto)
        {
            var milestone = await _projects.AddMilestoneAsync(id, dto);
            return StatusCode(StatusCodes.Status201Created, milestone.ToDto(Today));
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpPut("milestones/{milestoneId:int}")]
        public async Task<IActionResult> UpdateMilestone(int milestoneId, [FromBody] MilestoneUpdateDto dto)
        {
            var milestone = await _projects.UpdateMilestoneAsync(milestoneId, dto);
            if (milestone == null) return NotFound();
            return Ok(milestone.ToDto(Today));
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpPut("milestones/{milestoneId:int}/complete")]
        public async Task<IActionResult> CompleteMilestone(int milestoneId, [FromBody] CompleteMilestoneDto? dto)
        {
            var milestone = await _projects.CompleteMilestoneAsync(milestoneId, dto?.CompletedDate);
            if (milestone == null) return NotFound();
            return Ok(milestone.ToDto(Today));
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpDelete("milestones/{milestoneId:int}")]
        public async Task<IActionResult> DeleteMilestone(int milestoneId)
        {
            return await _projects.DeleteMilestoneAsync(milestoneId) ? NoContent() : NotFound();
        }
    }
}
