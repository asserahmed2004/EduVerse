using Application.DTOs.Certificates;
using Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResponse>Enroll(Guid courseId);
        Task<ServiceResponse> AddCertificate(CreateCertificate certificate);




    }
}
