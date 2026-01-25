using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagement.Core.DTOs.Teams;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;
using TaskManagement.Core.Exceptions;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        /// <summary>
        /// Create a new team (creator becomes owner)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TeamDetailDto>> CreateTeam([FromBody] CreateTeamRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var team = await _teamService.CreateTeamAsync(request, userId);
                return CreatedAtAction(nameof(GetTeamById), new { id = team.Id }, team);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Get team by ID (must be a member)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TeamDetailDto>> GetTeamById(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var team = await _teamService.GetTeamByIdAsync(id, userId);
                return Ok(team);
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
        /// Get all teams current user is member of
        /// </summary>
        [HttpGet("my-teams")]
        public async Task<ActionResult<List<TeamDto>>> GetMyTeams()
        {
            try
            {
                var userId = GetCurrentUserId();
                var teams = await _teamService.GetUserTeamsAsync(userId);
                return Ok(teams);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Update team (Owner or Manager only)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<TeamDetailDto>> UpdateTeam(int id, [FromBody] UpdateTeamRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var team = await _teamService.UpdateTeamAsync(id, request, userId);
                return Ok(team);
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
        /// Delete team (Owner only)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _teamService.DeleteTeamAsync(id, userId);
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
        /// Add member to team (Owner or Manager only)
        /// </summary>
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _teamService.AddMemberAsync(id, request, userId);
                return Ok(new { message = "Member added successfully" });
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
        /// Remove member from team (Owner or Manager only)
        /// </summary>
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _teamService.RemoveMemberAsync(id, userId, currentUserId);
                return Ok(new { message = "Member removed successfully" });
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
        /// Update member role (Owner only)
        /// </summary>
        [HttpPut("{id}/members/{userId}/role")]
        public async Task<IActionResult> UpdateMemberRole(int id, int userId, [FromBody] TeamMemberRole newRole)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _teamService.UpdateMemberRoleAsync(id, userId, newRole, currentUserId);
                return Ok(new { message = "Member role updated successfully" });
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
        /// Leave team (any member can leave, except last owner)
        /// </summary>
        [HttpPost("{id}/leave")]
        public async Task<IActionResult> LeaveTeam(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _teamService.LeaveTeamAsync(id, userId);
                return Ok(new { message = "Left team successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
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
