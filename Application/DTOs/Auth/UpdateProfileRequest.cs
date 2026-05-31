using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Auth
{
    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
}
