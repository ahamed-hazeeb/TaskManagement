using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.DTOs.Teams;

namespace TaskManagement.Application.Validators
{
    public class UpdateTeamRequestValidator : AbstractValidator<UpdateTeamRequest>
    {
        public UpdateTeamRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Team name is required")
                .MinimumLength(2).WithMessage("Team name must be at least 2 characters")
                .MaximumLength(100).WithMessage("Team name too long");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description too long")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}
