using Domain.Entities;
using Domain.Interfaces;
using InfraStructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraStructure.Repositories
{
    public class ConfirmationManagment(AppDbContext context) : IConfirmation
    {
        public async Task<bool> AddConfirmationCode(EmailConfirmation confirmation)
        {
            context.EmailConfirmations.Add(confirmation);
            var result =context.SaveChanges();
            return result > 0;

        }

        public async Task<EmailConfirmation> GetConfirmationByEmail(string email)
        {
            var confirmation = context.EmailConfirmations.FirstOrDefault(c => c.Email == email);
            return confirmation;
        }

        public async Task<bool> RemoveConfirmation(string email)
        {
            var confirmation = context.EmailConfirmations.FirstOrDefault(c => c.Email == email);
            if (confirmation == null)
            {
                return false;
            }
            context.EmailConfirmations.Remove(confirmation);
            var result = context.SaveChanges();
            return result > 0;
        }
    }
}
