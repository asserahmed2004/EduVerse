using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public record LoginResponse
        (
        bool succeed=false,
        string message=null,
        string token = null,
        string refreshToken = null
        );
}
