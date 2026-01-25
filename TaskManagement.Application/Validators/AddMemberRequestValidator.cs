using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.DTOs.Teams;

namespace TaskManagement.Application.Validators
{
    public class AddMemberRequestValidator : AbstractValidator<AddMemberRequest>
    {
        public AddMemberRequestValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Valid user ID is required");

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid role");
        }
    }
}
