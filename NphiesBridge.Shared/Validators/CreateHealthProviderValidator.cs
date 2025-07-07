using FluentValidation;
using NphiesBridge.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NphiesBridge.Shared.Validators
{
    public class CreateHealthProviderValidator : AbstractValidator<CreateHealthProviderDto>
    {
        public CreateHealthProviderValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Provider name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

            RuleFor(x => x.LicenseNumber)
                .NotEmpty().WithMessage("License number is required")
                .MaximumLength(50).WithMessage("License number cannot exceed 50 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Please provide a valid email address")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^[\d\s\+\-\(\)]+$").WithMessage("Please provide a valid phone number")
                .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters");

            RuleFor(x => x.ContactPerson)
                .MaximumLength(200).WithMessage("Contact person name cannot exceed 200 characters");
        }
    }

    public class UpdateHealthProviderValidator : AbstractValidator<UpdateHealthProviderDto>
    {
        public UpdateHealthProviderValidator()
        {
            Include(new CreateHealthProviderValidator());

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Provider ID is required");
        }
    }
}
