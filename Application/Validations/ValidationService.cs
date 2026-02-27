
using Application.DTOs.Responses;
using FluentValidation;

namespace Application.Validations
{
    public class ValidationService : IValidationService
    {
        public async Task<ServiceResponse> ValidateAsync<T>(T model, IValidator<T> validator)
        {
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return new ServiceResponse(false, string.Join("; ", errors));
            }
            return new ServiceResponse(true, "Validation succeeded");
        }
    }
}
