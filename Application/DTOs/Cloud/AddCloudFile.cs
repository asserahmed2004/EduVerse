using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Cloud
{
    public class AddCloudFile
    {
        public FileDetails Details { get; set; }
        public IFormFile File { get; set; }
    }
}
