using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Core.Enums
{
    public enum TeamMemberRole
    {
        Owner = 1,    // Created the team, full control
        Manager = 2,  // Can manage projects and tasks
        Member = 3    // Can only work on assigned tasks
    }
}
