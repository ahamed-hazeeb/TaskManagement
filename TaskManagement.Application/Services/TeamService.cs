using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.DTOs.Teams;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;
using TaskManagement.Core.Exceptions;
using TaskManagement.Core.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Application.Services
{
    public class TeamService : ITeamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;

        public TeamService(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        #region Team Operations

        public async Task<TeamDetailDto> CreateTeamAsync(CreateTeamRequest request, int currentUserId)
        {
            // Verify user exists
            var user = await _unitOfWork.Users.GetByIdAsync(currentUserId);
            if (user == null)
                throw new NotFoundException("User", currentUserId);

            // Create team
            var team = new Team
            {
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Teams.AddAsync(team);
            await _unitOfWork.SaveChangesAsync();

            // Add creator as Owner
            var teamMember = new TeamMember
            {
                TeamId = team.Id,
                UserId = currentUserId,
                Role = TeamMemberRole.Owner,
                JoinedAt = DateTime.UtcNow
            };

            await _unitOfWork.TeamMembers.AddAsync(teamMember);
            await _unitOfWork.SaveChangesAsync();

            // Return detailed team info
            return await GetTeamByIdAsync(team.Id, currentUserId);
        }

        public async Task<TeamDetailDto> GetTeamByIdAsync(int teamId, int currentUserId)
        {
            // Check if user is member of this team
            var isMember = await _unitOfWork.TeamMembers
                .ExistsAsync(tm => tm.TeamId == teamId && tm.UserId == currentUserId);

            if (!isMember)
                throw new UnauthorizedException("You are not a member of this team");

            // Get team with members (using EF Core Include for eager loading)
            var team = await _context.Teams
                .Include(t => t.Members)
                .ThenInclude(tm => tm.User)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                throw new NotFoundException("Team", teamId);

            return new TeamDetailDto
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                CreatedAt = team.CreatedAt,
                Members = team.Members.Select(tm => new TeamMemberDto
                {
                    Id = tm.Id,
                    UserId = tm.UserId,
                    UserName = tm.User.FullName,
                    UserEmail = tm.User.Email,
                    Role = tm.Role,
                    JoinedAt = tm.JoinedAt
                }).ToList()
            };
        }

        public async Task<List<TeamDto>> GetUserTeamsAsync(int userId)
        {
            // Get all teams user is member of
            var teamMemberships = await _context.TeamMembers
                .Include(tm => tm.Team)
                .Where(tm => tm.UserId == userId)
                .ToListAsync();

            var teamDtos = new List<TeamDto>();

            foreach (var membership in teamMemberships)
            {
                var memberCount = await _unitOfWork.TeamMembers
                    .CountAsync(tm => tm.TeamId == membership.TeamId);

                teamDtos.Add(new TeamDto
                {
                    Id = membership.Team.Id,
                    Name = membership.Team.Name,
                    Description = membership.Team.Description,
                    CreatedAt = membership.Team.CreatedAt,
                    MemberCount = memberCount
                });
            }

            return teamDtos;
        }

        public async Task<TeamDetailDto> UpdateTeamAsync(int teamId, UpdateTeamRequest request, int currentUserId)
        {
            // Check if team exists
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new NotFoundException("Team", teamId);

            // Check if user has permission (Owner or Manager)
            var userRole = await GetUserRoleInTeamAsync(teamId, currentUserId);
            if (userRole == null)
                throw new UnauthorizedException("You are not a member of this team");

            if (userRole != TeamMemberRole.Owner && userRole != TeamMemberRole.Manager)
                throw new UnauthorizedException("Only owners and managers can update teams");

            // Update team
            team.Name = request.Name;
            team.Description = request.Description;

            await _unitOfWork.Teams.UpdateAsync(team);
            await _unitOfWork.SaveChangesAsync();

            return await GetTeamByIdAsync(teamId, currentUserId);
        }

        public async Task DeleteTeamAsync(int teamId, int currentUserId)
        {
            // Check if team exists
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new NotFoundException("Team", teamId);

            // Check if user is Owner
            var userRole = await GetUserRoleInTeamAsync(teamId, currentUserId);
            if (userRole != TeamMemberRole.Owner)
                throw new UnauthorizedException("Only team owners can delete teams");

            // Delete team (cascade will delete members, projects, tasks)
            await _unitOfWork.Teams.DeleteAsync(team);
            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region Member Operations

        public async Task AddMemberAsync(int teamId, AddMemberRequest request, int currentUserId)
        {
            // Check if team exists
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new NotFoundException("Team", teamId);

            // Check if current user has permission (Owner or Manager)
            var currentUserRole = await GetUserRoleInTeamAsync(teamId, currentUserId);
            if (currentUserRole != TeamMemberRole.Owner && currentUserRole != TeamMemberRole.Manager)
                throw new UnauthorizedException("Only owners and managers can add members");

            // Check if user to add exists
            var userToAdd = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            if (userToAdd == null)
                throw new NotFoundException("User", request.UserId);

            // Check if user is already a member
            var existingMembership = await _unitOfWork.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == request.UserId);

            if (existingMembership != null)
                throw new BadRequestException("User is already a member of this team");

            // Business rule: Only owners can add managers
            if (request.Role == TeamMemberRole.Manager && currentUserRole != TeamMemberRole.Owner)
                throw new UnauthorizedException("Only owners can add managers");

            // Add member
            var teamMember = new TeamMember
            {
                TeamId = teamId,
                UserId = request.UserId,
                Role = request.Role,
                JoinedAt = DateTime.UtcNow
            };

            await _unitOfWork.TeamMembers.AddAsync(teamMember);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(int teamId, int memberUserId, int currentUserId)
        {
            // Check if team exists
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new NotFoundException("Team", teamId);

            // Check if current user has permission
            var currentUserRole = await GetUserRoleInTeamAsync(teamId, currentUserId);
            if (currentUserRole != TeamMemberRole.Owner && currentUserRole != TeamMemberRole.Manager)
                throw new UnauthorizedException("Only owners and managers can remove members");

            // Get member to remove
            var memberToRemove = await _unitOfWork.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == memberUserId);

            if (memberToRemove == null)
                throw new NotFoundException("Member not found in this team");

            // Business rule: Can't remove owner (they must leave themselves or transfer ownership)
            if (memberToRemove.Role == TeamMemberRole.Owner)
                throw new BadRequestException("Cannot remove team owner. Owner must leave team themselves.");

            // Business rule: Managers can't remove other managers
            if (memberToRemove.Role == TeamMemberRole.Manager && currentUserRole == TeamMemberRole.Manager)
                throw new UnauthorizedException("Managers cannot remove other managers");

            // Remove member
            await _unitOfWork.TeamMembers.DeleteAsync(memberToRemove);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateMemberRoleAsync(int teamId, int memberUserId, TeamMemberRole newRole, int currentUserId)
        {
            // Check if team exists
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new NotFoundException("Team", teamId);

            // Only owners can change roles
            var currentUserRole = await GetUserRoleInTeamAsync(teamId, currentUserId);
            if (currentUserRole != TeamMemberRole.Owner)
                throw new UnauthorizedException("Only team owners can change member roles");

            // Get member to update
            var member = await _unitOfWork.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == memberUserId);

            if (member == null)
                throw new NotFoundException("Member not found in this team");

            // Business rule: Can't change owner's role (must transfer ownership)
            if (member.Role == TeamMemberRole.Owner)
                throw new BadRequestException("Cannot change owner's role. Use transfer ownership instead.");

            // Update role
            member.Role = newRole;
            await _unitOfWork.TeamMembers.UpdateAsync(member);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task LeaveTeamAsync(int teamId, int currentUserId)
        {
            // Check if team exists
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new NotFoundException("Team", teamId);

            // Get user's membership
            var membership = await _unitOfWork.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == currentUserId);

            if (membership == null)
                throw new NotFoundException("You are not a member of this team");

            // Business rule: If user is owner, check if there are other owners
            if (membership.Role == TeamMemberRole.Owner)
            {
                var ownerCount = await _unitOfWork.TeamMembers
                    .CountAsync(tm => tm.TeamId == teamId && tm.Role == TeamMemberRole.Owner);

                if (ownerCount == 1)
                    throw new BadRequestException("Cannot leave team. You are the only owner. Transfer ownership first or delete the team.");
            }

            // Remove membership
            await _unitOfWork.TeamMembers.DeleteAsync(membership);
            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

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
