using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Core.DTOs.Tasks
{
    public class UpdateTaskStatusRequest
    {
        public TaskStatus Status { get; set; }
    }
}
