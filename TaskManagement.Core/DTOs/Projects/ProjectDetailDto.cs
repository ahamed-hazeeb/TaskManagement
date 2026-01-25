using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.DTOs.Tasks;


namespace TaskManagement.Core.DTOs.Projects
{
    public class ProjectDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public List<TaskDto> Tasks { get; set; } = new();
    }
}
