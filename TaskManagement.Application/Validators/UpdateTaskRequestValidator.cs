using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.DTOs.Tasks;

namespace TaskManagement.Application.Validators
{
    public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
    {
        public UpdateTaskRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Task title is required")
                .MinimumLength(2).WithMessage("Task title must be at least 2 characters")
                .MaximumLength(200).WithMessage("Task title too long");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description too long")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Invalid priority");

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future")
                .When(x => x.DueDate.HasValue);
        }
    }
}
