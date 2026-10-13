using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    /// <summary>The standard stage list (order, owner, default days) that new projects are built from.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StagesController : ControllerBase
    {
        private readonly IStageService _stages;

        public StagesController(IStageService stages) => _stages = stages;

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok((await _stages.GetAllAsync()).Select(s => s.ToDto()));

        [Authorize(Roles = Roles.Owner)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StageSaveDto dto) =>
            StatusCode(StatusCodes.Status201Created, (await _stages.CreateAsync(dto)).ToDto());

        [Authorize(Roles = Roles.Owner)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] StageSaveDto dto)
        {
            var stage = await _stages.UpdateAsync(id, dto);
            return stage == null ? NotFound() : Ok(stage.ToDto());
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id) =>
            await _stages.DeleteAsync(id) ? NoContent() : NotFound();

        [Authorize(Roles = Roles.Owner)]
        [HttpPut("{id:int}/move")]
        public async Task<IActionResult> Move(int id, [FromQuery] string direction)
        {
            if (direction != "up" && direction != "down") return BadRequest("direction must be 'up' or 'down'.");
            return await _stages.MoveAsync(id, direction == "up") ? NoContent() : NotFound();
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpPost("seed-defaults")]
        public async Task<IActionResult> SeedDefaults()
        {
            if (!await _stages.SeedDefaultsAsync())
                return Conflict("Stages already exist. Delete them first to reload the standard list.");
            return Ok((await _stages.GetAllAsync()).Select(s => s.ToDto()));
        }
    }
}
