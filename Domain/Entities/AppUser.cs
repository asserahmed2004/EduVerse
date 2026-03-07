using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class AppUser:IdentityUser
    {
        public string FullName { get; set; }=string.Empty;
        public string? ProfilePicture { get; set; }= string.Empty;
        public DateOnly Birthdate { get; set; }


    }

}
