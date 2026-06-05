using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Assignment
{
    public class GetAssignment
    {
        public Guid Id { get; set; } 
        public Guid SessionId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
