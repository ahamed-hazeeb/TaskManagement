using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IRepository<User> Users { get; }
        public IRepository<Team> Teams { get; }
        public IRepository<TeamMember> TeamMembers { get; }
        public IRepository<Project> Projects { get; }
        public IRepository<ProjectTask> Tasks { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            // Initialize all repositories
            Users = new Repository<User>(_context);
            Teams = new Repository<Team>(_context);
            TeamMembers = new Repository<TeamMember>(_context);
            Projects = new Repository<Project>(_context);
            Tasks = new Repository<ProjectTask>(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }

}
