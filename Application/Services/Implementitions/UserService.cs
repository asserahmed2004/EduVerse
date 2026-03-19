using Application.DTOs.Certificates;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implementitions
{
    public class UserService : IUserService
    {
        public Task<ServiceResponse> AddCertificate(CreateCertificate certificate)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse> Enroll(Guid courseId)
        {
            throw new NotImplementedException();
        }
    }
}
