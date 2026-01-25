using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Core.DTOs.Projects
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public int TaskCount { get; set; }
    }
}
