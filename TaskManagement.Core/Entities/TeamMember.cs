using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.Enums;

namespace TaskManagement.Core.Entities
{
    public class TeamMember
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public int UserId { get; set; }
        public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Team Team { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
