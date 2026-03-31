using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Models;
using RealEstateCRM.Models.DTOs;
using RealEstateCRM.Services;

namespace RealEstateCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ICrmTaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ICrmTaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] CrmTaskStatus? status = null)
        {
            try
            {
                var tasks = await _taskService.GetAllAsync(status);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll tasks");
                return StatusCode(500, "An error occurred while retrieving tasks.");
            }
        }

        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdue()
        {
            try
            {
                var tasks = await _taskService.GetOverdueAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue tasks");
                return StatusCode(500, "An error occurred while retrieving overdue tasks.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var task = await _taskService.GetByIdAsync(id);
                if (task == null) return NotFound();
                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetById task {TaskId}", id);
                return StatusCode(500, "An error occurred while retrieving the task.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrmTaskCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var task = await _taskService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, "An error occurred while creating the task.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CrmTaskUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var task = await _taskService.UpdateAsync(id, dto);
                if (task == null) return NotFound();
                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", id);
                return StatusCode(500, "An error occurred while updating the task.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _taskService.DeleteAsync(id);
                if (!result) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {TaskId}", id);
                return StatusCode(500, "An error occurred while deleting the task.");
            }
        }
    }
}
