using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validations.Auth
{
    public class RegisterValidation: AbstractValidator<RegisterUser>
    {
        public RegisterValidation()
        {
            RuleFor(x => x.FullName).NotEmpty().WithMessage("Full name is required");
            RuleFor(x => x.UserName).NotEmpty().WithMessage("Username is required");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Invalid email format");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
                .Matches("[a-z]").WithMessage("Password must contain a lowercase letter")
                .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter")
                .Matches("[0-9]").WithMessage("Password must contain a number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a symbol");
            RuleFor(x => x.confirmPassword).Equal(x => x.Password).WithMessage("Passwords do not match");
            RuleFor(x => x.Birth)
                .NotEmpty().WithMessage("Birth date is required")
                .Must(value => DateOnly.TryParse(value, out _)).WithMessage("Birth date is invalid");
            RuleFor(x => x.role)
                .NotEmpty().WithMessage("Role is required")
                .Must(role => role != null && new[] { "student", "instructor" }
                    .Contains(role.Trim().ToLowerInvariant()))
                .WithMessage("Role must be Student or Instructor");
        }
    }
}
