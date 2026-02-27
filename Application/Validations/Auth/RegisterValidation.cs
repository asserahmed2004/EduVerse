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
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters long");
            RuleFor(x => x.confirmPassword).Equal(x => x.Password).WithMessage("Passwords do not match");
        }
    }
}
