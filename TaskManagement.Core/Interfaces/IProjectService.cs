using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.DTOs.Projects;

namespace TaskManagement.Core.Interfaces
{
    public interface IProjectService
    {
        Task<ProjectDetailDto> CreateProjectAsync(int teamId, CreateProjectRequest request, int currentUserId);
        Task<ProjectDetailDto> GetProjectByIdAsync(int projectId, int currentUserId);
        Task<List<ProjectDto>> GetTeamProjectsAsync(int teamId, int currentUserId);
        Task<ProjectDetailDto> UpdateProjectAsync(int projectId, UpdateProjectRequest request, int currentUserId);
        Task DeleteProjectAsync(int projectId, int currentUserId);
    }
}
