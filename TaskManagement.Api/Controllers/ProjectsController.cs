using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagement.Core.DTOs.Projects;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Exceptions;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/teams/{teamId}/projects")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        /// <summary>
        /// Create a new project in team
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ProjectDetailDto>> CreateProject(int teamId, [FromBody] CreateProjectRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var project = await _projectService.CreateProjectAsync(teamId, request, userId);
                return CreatedAtAction(nameof(GetProjectById), new { teamId, id = project.Id }, project);
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
        /// Get project by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectDetailDto>> GetProjectById(int teamId, int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var project = await _projectService.GetProjectByIdAsync(id, userId);
                return Ok(project);
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
        /// Get all projects in team
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ProjectDto>>> GetTeamProjects(int teamId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var projects = await _projectService.GetTeamProjectsAsync(teamId, userId);
                return Ok(projects);
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
        /// Update project
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ProjectDetailDto>> UpdateProject(int teamId, int id, [FromBody] UpdateProjectRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var project = await _projectService.UpdateProjectAsync(id, request, userId);
                return Ok(project);
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
        /// Delete project
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int teamId, int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _projectService.DeleteProjectAsync(id, userId);
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

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("Invalid token");

            return userId;
        }

        #endregion
    }
}
