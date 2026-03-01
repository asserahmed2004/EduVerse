using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class EmailConfirmation
    {
        public string Email { get; set; }
        public string ConfirmationCode { get; set; }
    }
}
