using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Submission
{
    public class GetAssignmentSubmission
    {
        public string StudentId { get; set; }
        public Guid AssignmentId { get; set; }
        public string FileUrl { get; set; }
    }
}
