using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.DTOs.Tasks;
using TaskStatus = TaskManagement.Core.Enums.TaskStatus;  


namespace TaskManagement.Core.Interfaces
{
    public interface ITaskService
    {
        Task<TaskDto> CreateTaskAsync(int projectId, CreateTaskRequest request, int currentUserId);
        Task<TaskDto> GetTaskByIdAsync(int taskId, int currentUserId);
        Task<List<TaskDto>> GetProjectTasksAsync(int projectId, int currentUserId);
        Task<TaskDto> UpdateTaskAsync(int taskId, UpdateTaskRequest request, int currentUserId);
        Task DeleteTaskAsync(int taskId, int currentUserId);
        Task<TaskDto> AssignTaskAsync(int taskId, int userId, int currentUserId);
        Task<TaskDto> UnassignTaskAsync(int taskId, int currentUserId);
        Task<TaskDto> UpdateTaskStatusAsync(int taskId, TaskStatus newStatus, int currentUserId);
    }
}
