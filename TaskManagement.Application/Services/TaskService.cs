using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.DTOs.Common;
using TaskManagement.Core.DTOs.Tasks;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;
using TaskManagement.Core.Exceptions;
using TaskManagement.Core.Interfaces;
using TaskManagement.Infrastructure.Data;
using TaskState = TaskManagement.Core.Enums.TaskState;

namespace TaskManagement.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;

        public TaskService(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<TaskDto> CreateTaskAsync(int projectId, CreateTaskRequest request, int currentUserId)
        {
            // Get project with team
            var project = await _context.Projects
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new NotFoundException("Project", projectId);

            // Verify user is member of team
            var isMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == project.TeamId && tm.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedException("You must be a team member to create tasks");

            // If assigning to someone, verify they are team member
            if (request.AssignedToUserId.HasValue)
            {
                var assigneeIsMember = await _unitOfWork.TeamMembers
                    .ExistsAsync(tm => tm.TeamId == project.TeamId && tm.UserId == request.AssignedToUserId.Value);

                if (!assigneeIsMember)
                    throw new BadRequestException("Can only assign tasks to team members");
            }

            // Create task
            var task = new ProjectTask
            {
                Title = request.Title,
                Description = request.Description,
                Status = TaskState.Todo,
                Priority = request.Priority,
                DueDate = request.DueDate,
                AssignedToUserId = request.AssignedToUserId,
                ProjectId = projectId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Tasks.AddAsync(task);
            await _unitOfWork.SaveChangesAsync();

            return await GetTaskByIdAsync(task.Id, currentUserId);
        }

        public async Task<TaskDto> GetTaskByIdAsync(int taskId, int currentUserId)
        {
            // Get task with related data
            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Project)
                    .ThenInclude(p => p.Team)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new NotFoundException("Task", taskId);

            // Verify user is member of the team
            var isMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == task.Project.TeamId && tm.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedException("You must be a team member to view this task");

            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                AssignedToUserId = task.AssignedToUserId,
                AssignedToUserName = task.AssignedTo?.FullName,
                ProjectId = task.ProjectId,
                ProjectName = task.Project.Name,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt
            };
        }

        public async Task<List<TaskDto>> GetProjectTasksAsync(int projectId, int currentUserId)
        {
            // Get project
            var project = await _context.Projects
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new NotFoundException("Project", projectId);

            // Verify user is member of team
            var isMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == project.TeamId && tm.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedException("You must be a team member to view tasks");

            // Get all tasks for this project
            var tasks = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Project)
                .Where(t => t.ProjectId == projectId)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            return tasks.Select(t => new TaskDto
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
                ProjectName = t.Project.Name,
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt
            }).ToList();
        }
        public async Task<PagedResult<TaskDto>> GetProjectTasksPagedAsync(int projectId, TaskQueryParams queryParams, int currentUserId)
        {
            // Get project and verify access
            var project = await _context.Projects
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new NotFoundException("Project", projectId);

            // Verify user is member of team
            var isMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == project.TeamId && tm.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedException("You must be a team member to view tasks");

            // Start building query
            var query = _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Project)
                .Where(t => t.ProjectId == projectId)
                .AsQueryable();

            // Apply filters
            if (queryParams.Status.HasValue)
            {
                query = query.Where(t => t.Status == queryParams.Status.Value);
            }

            if (queryParams.Priority.HasValue)
            {
                query = query.Where(t => t.Priority == queryParams.Priority.Value);
            }

            if (queryParams.AssignedToUserId.HasValue)
            {
                query = query.Where(t => t.AssignedToUserId == queryParams.AssignedToUserId.Value);
            }

            if (queryParams.DueDateFrom.HasValue)
            {
                query = query.Where(t => t.DueDate >= queryParams.DueDateFrom.Value);
            }

            if (queryParams.DueDateTo.HasValue)
            {
                query = query.Where(t => t.DueDate <= queryParams.DueDateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
            {
                var searchTerm = queryParams.SearchTerm.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(searchTerm) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchTerm)));
            }

            // Apply sorting
            query = queryParams.SortBy?.ToLower() switch
            {
                "priority" => queryParams.SortDescending
                    ? query.OrderByDescending(t => t.Priority)
                    : query.OrderBy(t => t.Priority),

                "duedate" => queryParams.SortDescending
                    ? query.OrderByDescending(t => t.DueDate)
                    : query.OrderBy(t => t.DueDate),

                "createdat" => queryParams.SortDescending
                    ? query.OrderByDescending(t => t.CreatedAt)
                    : query.OrderBy(t => t.CreatedAt),

                _ => query.OrderBy(t => t.CreatedAt)  // Default sort
            };

            // Get total count BEFORE pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var tasks = await query
                .Skip(queryParams.Skip)
                .Take(queryParams.PageSize)
                .ToListAsync();

            // Map to DTOs
            var taskDtos = tasks.Select(t => new TaskDto
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
                ProjectName = t.Project.Name,
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt
            }).ToList();

            // Return paged result
            return new PagedResult<TaskDto>
            {
                Items = taskDtos,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize)
            };
        }
        public async Task<TaskDto> UpdateTaskAsync(int taskId, UpdateTaskRequest request, int currentUserId)
        {
            // Get task
            var task = await _context.Tasks
                .Include(t => t.Project)
                    .ThenInclude(p => p.Team)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new NotFoundException("Task", taskId);

            // Verify user is member of team
            var isMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == task.Project.TeamId && tm.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedException("You must be a team member to update tasks");

            // Update task
            task.Title = request.Title;
            task.Description = request.Description;
            task.Priority = request.Priority;
            task.DueDate = request.DueDate;

            await _unitOfWork.Tasks.UpdateAsync(task);
            await _unitOfWork.SaveChangesAsync();

            return await GetTaskByIdAsync(taskId, currentUserId);
        }

        public async Task DeleteTaskAsync(int taskId, int currentUserId)
        {
            // Get task
            var task = await _context.Tasks
                .Include(t => t.Project)
                    .ThenInclude(p => p.Team)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new NotFoundException("Task", taskId);

            // Check user permission (Owner or Manager)
            var userRole = await GetUserRoleInTeamAsync(task.Project.TeamId, currentUserId);
            if (userRole != TeamMemberRole.Owner && userRole != TeamMemberRole.Manager)
                throw new UnauthorizedException("Only owners and managers can delete tasks");

            // Delete task
            await _unitOfWork.Tasks.DeleteAsync(task);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<TaskDto> AssignTaskAsync(int taskId, int userId, int currentUserId)
        {
            // Get task
            var task = await _context.Tasks
                .Include(t => t.Project)
                    .ThenInclude(p => p.Team)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new NotFoundException("Task", taskId);

            // Verify current user is member of team
            var currentUserIsMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == task.Project.TeamId && tm.UserId == currentUserId);

            if (!currentUserIsMember)
                throw new UnauthorizedException("You must be a team member to assign tasks");

            // Verify assignee is member of team
            var assigneeIsMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == task.Project.TeamId && tm.UserId == userId);

            if (!assigneeIsMember)
                throw new BadRequestException("Can only assign tasks to team members");

            // Assign task
            task.AssignedToUserId = userId;

            await _unitOfWork.Tasks.UpdateAsync(task);
            await _unitOfWork.SaveChangesAsync();

            return await GetTaskByIdAsync(taskId, currentUserId);
        }

        public async Task<TaskDto> UnassignTaskAsync(int taskId, int currentUserId)
        {
            // Get task
            var task = await _context.Tasks
                .Include(t => t.Project)
                    .ThenInclude(p => p.Team)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new NotFoundException("Task", taskId);

            // Verify user is member of team
            var isMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == task.Project.TeamId && tm.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedException("You must be a team member to unassign tasks");

            // Unassign task
            task.AssignedToUserId = null;

            await _unitOfWork.Tasks.UpdateAsync(task);
            await _unitOfWork.SaveChangesAsync();

            return await GetTaskByIdAsync(taskId, currentUserId);
        }

        public async Task<TaskDto> UpdateTaskStatusAsync(int taskId, TaskState newStatus, int currentUserId)
        {
            // Get task
            var task = await _context.Tasks
                .Include(t => t.Project)
                    .ThenInclude(p => p.Team)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new NotFoundException("Task", taskId);

            // Verify user is member of team
            var isMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == task.Project.TeamId && tm.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedException("You must be a team member to update task status");

            // Update status
            task.Status = newStatus;

            // If marking as Done, set CompletedAt
            if (newStatus == TaskState.Done && task.CompletedAt == null)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            // If moving back from Done, clear CompletedAt
            else if (newStatus != TaskState.Done && task.CompletedAt != null)
            {
                task.CompletedAt = null;
            }

            await _unitOfWork.Tasks.UpdateAsync(task);
            await _unitOfWork.SaveChangesAsync();

            return await GetTaskByIdAsync(taskId, currentUserId);
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
