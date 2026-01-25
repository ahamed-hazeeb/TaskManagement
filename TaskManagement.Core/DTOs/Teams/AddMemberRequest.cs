using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.Enums;

namespace TaskManagement.Core.DTOs.Teams
{
    public class AddMemberRequest
    {
        public int UserId { get; set; }
        public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;
    }
}
