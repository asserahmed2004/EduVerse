using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Auth
{
    public class GetUser
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string? ProfilePicture { get; set; }
        public DateOnly BirthDate { get; set; }

        public string phoneNumber { get; set; }
        
        public string role { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        
    }
}
