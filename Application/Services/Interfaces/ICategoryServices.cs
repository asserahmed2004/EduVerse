using Application.DTOs.Category;
using Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<ServiceResponse> CreateCategory(CreateCategory category);
        Task<ServiceResponse> UpdateCategory(UpdateCategory category);
        Task<ServiceResponse> DeleteCategory(Guid id);
        Task<List<GetCategory>> GetAllCategories();
        Task<GetCategory> GetCategoryById(Guid id);
        Task<GetCategory> GetCategoryByName(string name);

    }
}
