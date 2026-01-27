using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagement.Core.DTOs.Common;
using TaskManagement.Core.DTOs.Tasks;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Exceptions;
using TaskManagement.Core.Interfaces;


namespace TaskManagement.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/projects/{projectId}/tasks")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        /// <summary>
        /// Create a new task in project
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TaskDto>> CreateTask(int projectId, [FromBody] CreateTaskRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var task = await _taskService.CreateTaskAsync(projectId, request, userId);
                return CreatedAtAction(nameof(GetTaskById), new { projectId, id = task.Id }, task);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Get task by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTaskById(int projectId, int id)
        {
            var userId = GetCurrentUserId();
            var task = await _taskService.GetTaskByIdAsync(id, userId);
            return Ok(task);
        }

        /// <summary>
        /// Get all tasks in project
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<TaskDto>>> GetProjectTasks(int projectId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetProjectTasksAsync(projectId, userId);
                return Ok(tasks);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Update task
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskDto>> UpdateTask(int projectId, int id, [FromBody] UpdateTaskRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var task = await _taskService.UpdateTaskAsync(id, request, userId);
                return Ok(task);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete task
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int projectId, int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _taskService.DeleteTaskAsync(id, userId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Assign task to user
        /// </summary>
        [HttpPost("{id}/assign")]
        public async Task<ActionResult<TaskDto>> AssignTask(int projectId, int id, [FromBody] AssignTaskRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var task = await _taskService.AssignTaskAsync(id, request.UserId, userId);
                return Ok(task);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Unassign task
        /// </summary>
        [HttpPost("{id}/unassign")]
        public async Task<ActionResult<TaskDto>> UnassignTask(int projectId, int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var task = await _taskService.UnassignTaskAsync(id, userId);
                return Ok(task);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }
        /// <summary>
        /// Get all tasks in project with filtering, sorting, and pagination
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<TaskDto>>> GetProjectTasksPaged(
            int projectId,
            [FromQuery] TaskQueryParams queryParams)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _taskService.GetProjectTasksPagedAsync(projectId, queryParams, userId);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }
        /// <summary>
        /// Update task status
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<TaskDto>> UpdateTaskStatus(int projectId, int id, [FromBody] UpdateTaskStatusRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var task = await _taskService.UpdateTaskStatusAsync(id, request.Status, userId);
                return Ok(task);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("Invalid token");

            return userId;
        }

        #endregion
    }
}
