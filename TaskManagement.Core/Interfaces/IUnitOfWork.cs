using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.Entities;

namespace TaskManagement.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IRepository<Team> Teams { get; }
        IRepository<TeamMember> TeamMembers { get; }
        IRepository<Project> Projects { get; }
        IRepository<ProjectTask> Tasks { get; }

        Task<int> SaveChangesAsync();
    }
}
