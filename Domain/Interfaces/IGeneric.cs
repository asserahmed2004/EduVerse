using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IGeneric<TEntity> where TEntity : class
    {
        IQueryable<TEntity> Query(bool tracking = false);
        Task<TEntity> GetByIdAsync(Guid id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity> AddAsync(TEntity entity);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task<int> DeleteAsync(TEntity entity);
        Task<int> DeleteAsync(Guid id);
        Task<int> MassAdd(IEnumerable<TEntity> entities);
    }
}
