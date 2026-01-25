using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.DTOs.Projects;
using TaskManagement.Core.DTOs.Tasks;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;
using TaskManagement.Core.Exceptions;
using TaskManagement.Core.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;

        public ProjectService(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<ProjectDetailDto> CreateProjectAsync(int teamId, CreateProjectRequest request, int currentUserId)
        {
            // Verify team exists
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new NotFoundException("Team", teamId);

            // Verify user is member of team
            var isMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == teamId && tm.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedException("You must be a team member to create projects");

            // Create project
            var project = new Project
            {
                Name = request.Name,
                Description = request.Description,
                TeamId = teamId,
                Deadline = request.Deadline,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Projects.AddAsync(project);
            await _unitOfWork.SaveChangesAsync();

            return await GetProjectByIdAsync(project.Id, currentUserId);
        }

        public async Task<ProjectDetailDto> GetProjectByIdAsync(int projectId, int currentUserId)
        {
            // Get project with team and tasks
            var project = await _context.Projects
                .Include(p => p.Team)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.AssignedTo)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new NotFoundException("Project", projectId);

            // Verify user is member of the team
            var isMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == project.TeamId && tm.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedException("You must be a team member to view this project");

            return new ProjectDetailDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                TeamId = project.TeamId,
                TeamName = project.Team.Name,
                CreatedAt = project.CreatedAt,
                Deadline = project.Deadline,
                Tasks = project.Tasks.Select(t => new TaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToUserName = t.AssignedTo?.FullName,
                    ProjectId = t.ProjectId,
                    ProjectName = project.Name,
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt
                }).ToList()
            };
        }

        public async Task<List<ProjectDto>> GetTeamProjectsAsync(int teamId, int currentUserId)
        {
            // Verify team exists
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new NotFoundException("Team", teamId);

            // Verify user is member of team
            var isMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == teamId && tm.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedException("You must be a team member to view projects");

            // Get all projects for this team
            var projects = await _context.Projects
                .Include(p => p.Team)
                .Where(p => p.TeamId == teamId)
                .ToListAsync();

            var projectDtos = new List<ProjectDto>();

            foreach (var project in projects)
            {
                var taskCount = await _unitOfWork.Tasks
                    .CountAsync(t => t.ProjectId == project.Id);

                projectDtos.Add(new ProjectDto
                {
                    Id = project.Id,
                    Name = project.Name,
                    Description = project.Description,
                    TeamId = project.TeamId,
                    TeamName = project.Team.Name,
                    CreatedAt = project.CreatedAt,
                    Deadline = project.Deadline,
                    TaskCount = taskCount
                });
            }

            return projectDtos;
        }

        public async Task<ProjectDetailDto> UpdateProjectAsync(int projectId, UpdateProjectRequest request, int currentUserId)
        {
            // Get project
            var project = await _context.Projects
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new NotFoundException("Project", projectId);

            // Check user permission (Owner or Manager)
            var userRole = await GetUserRoleInTeamAsync(project.TeamId, currentUserId);
            if (userRole == null)
                throw new UnauthorizedException("You are not a member of this team");

            if (userRole != TeamMemberRole.Owner && userRole != TeamMemberRole.Manager)
                throw new UnauthorizedException("Only owners and managers can update projects");

            // Update project
            project.Name = request.Name;
            project.Description = request.Description;
            project.Deadline = request.Deadline;

            await _unitOfWork.Projects.UpdateAsync(project);
            await _unitOfWork.SaveChangesAsync();

            return await GetProjectByIdAsync(projectId, currentUserId);
        }

        public async Task DeleteProjectAsync(int projectId, int currentUserId)
        {
            // Get project
            var project = await _context.Projects
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new NotFoundException("Project", projectId);

            // Check user permission (Owner or Manager)
            var userRole = await GetUserRoleInTeamAsync(project.TeamId, currentUserId);
            if (userRole == null)
                throw new UnauthorizedException("You are not a member of this team");

            if (userRole != TeamMemberRole.Owner && userRole != TeamMemberRole.Manager)
                throw new UnauthorizedException("Only owners and managers can delete projects");

            // Delete project (cascade will delete tasks)
            await _unitOfWork.Projects.DeleteAsync(project);
            await _unitOfWork.SaveChangesAsync();
        }

        #region Helper Methods

        private async Task<TeamMemberRole?> GetUserRoleInTeamAsync(int teamId, int userId)
        {
            var membership = await _unitOfWork.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);

            return membership?.Role;
        }

        #endregion
    }
}
