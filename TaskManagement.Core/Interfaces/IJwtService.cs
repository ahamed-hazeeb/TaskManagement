using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.Entities;

namespace TaskManagement.Core.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        int? ValidateToken(string token);
    }
}
