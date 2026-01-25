using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.Enums;

namespace TaskManagement.Core.DTOs.Tasks
{
    public class UpdateTaskStatusRequest
    {
        public TaskState Status { get; set; }
    }
}
