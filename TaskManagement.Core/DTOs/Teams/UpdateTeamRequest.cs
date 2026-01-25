using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Core.DTOs.Teams
{
    public class UpdateTeamRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
