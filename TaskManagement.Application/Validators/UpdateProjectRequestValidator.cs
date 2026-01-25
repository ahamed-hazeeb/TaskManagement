using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.DTOs.Projects;

namespace TaskManagement.Application.Validators
{
    public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
    {
        public UpdateProjectRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name is required")
                .MinimumLength(2).WithMessage("Project name must be at least 2 characters")
                .MaximumLength(100).WithMessage("Project name too long");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description too long")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Deadline)
                .GreaterThan(DateTime.UtcNow).WithMessage("Deadline must be in the future")
                .When(x => x.Deadline.HasValue);
        }
    }
}
