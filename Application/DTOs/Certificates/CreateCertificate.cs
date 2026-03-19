using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Certificates
{
    public class CreateCertificate
    {
        public Guid CourseId { get; set; }
        public string? UserId { get; set; }
        IFormFile? CertificateFile { get; set; }
    }
}
