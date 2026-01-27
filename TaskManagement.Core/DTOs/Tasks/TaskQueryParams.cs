using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.DTOs.Common;
using TaskManagement.Core.Enums;

namespace TaskManagement.Core.DTOs.Tasks
{
    public class TaskQueryParams : PaginationParams
    {
        // Filtering
        public TaskState? Status { get; set; }
        public TaskPriority? Priority { get; set; }
        public int? AssignedToUserId { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public string? SearchTerm { get; set; }

        // Sorting
        public string? SortBy { get; set; }  // "priority", "dueDate", "createdAt"
        public bool SortDescending { get; set; } = false;
    }
}
