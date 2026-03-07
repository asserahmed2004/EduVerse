using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Cloud
{
    public class GetCloudFile
    {
        public FileDetails Details { get; set; }
        
        public Stream FileStream { get; set; }
    }
}
