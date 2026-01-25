using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.DTOs.Teams;
using TaskManagement.Core.Enums;

namespace TaskManagement.Core.Interfaces
{
    public interface ITeamService
    {
        // Team operations
        Task<TeamDetailDto> CreateTeamAsync(CreateTeamRequest request, int currentUserId);
        Task<TeamDetailDto> GetTeamByIdAsync(int teamId, int currentUserId);
        Task<List<TeamDto>> GetUserTeamsAsync(int userId);
        Task<TeamDetailDto> UpdateTeamAsync(int teamId, UpdateTeamRequest request, int currentUserId);
        Task DeleteTeamAsync(int teamId, int currentUserId);

        // Member operations
        Task AddMemberAsync(int teamId, AddMemberRequest request, int currentUserId);
        Task RemoveMemberAsync(int teamId, int memberUserId, int currentUserId);
        Task UpdateMemberRoleAsync(int teamId, int memberUserId, TeamMemberRole newRole, int currentUserId);
        Task LeaveTeamAsync(int teamId, int currentUserId);
    }
}
