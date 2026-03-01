using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IConfirmation
    {
        public Task<bool> AddConfirmationCode(EmailConfirmation confirmation);
        public Task<EmailConfirmation> GetConfirmationByEmail(string email);
        public Task<bool> RemoveConfirmation(string email);
    }
}
