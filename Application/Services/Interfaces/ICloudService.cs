using Application.DTOs.Cloud;
using Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface ICloudService
    {
        public Task<ServiceResponse> UploadFileAsync(AddCloudFile cloudFile);
        public Task<GetCloudFile> GetFileAsync(FileDetails details);
        public Task<ServiceResponse> DeleteFileAsync(FileDetails details);
    }
}
