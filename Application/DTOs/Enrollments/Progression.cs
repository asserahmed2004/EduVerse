using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Enrollments
{
    public class Progression
    {
        public Guid CourseId { get; set; }
        public string Email { get; set; }
        public double ProgressionValue { get; set; }
        
    }
}
