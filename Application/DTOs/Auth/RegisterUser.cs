using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Auth
{
    public class RegisterUser
    {
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string confirmPassword { get; set; }
        public string phoneNumber { get; set; } 
        public string Birth { get; set; }
        public IFormFile? ProfilePicture { get; set; }


        public string role { get; set; }
        public string ConfirmationCode { get; set; }


    }
}
