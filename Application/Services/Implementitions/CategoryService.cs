using Application.DTOs.Category;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implementitions
{
    public class CategoryService(IGeneric<Category> Repo , IMapper mapper) : ICategoryService
    {
        public async Task<ServiceResponse> CreateCategory(CreateCategory category)
        {
            if (category == null)
            {
                return new ServiceResponse(false, "Category cannot be null.");
            }
            var newCategory = mapper.Map<Category>(category);
            var result = await Repo.AddAsync(newCategory);
            if (result !=null)
            {
                return new ServiceResponse(true, "Category created successfully.");
            }
            else
            {
                return new ServiceResponse(false, "Failed to create category.");
            }
        }

        public async Task<ServiceResponse> DeleteCategory(Guid id)
        {
            if (id == Guid.Empty)
            {
                return new ServiceResponse(false, "Invalid category ID.");
            }
            var result = await Repo.DeleteAsync(id);
            if (result > 0)
            {
                return new ServiceResponse(true, "Category deleted successfully.");
            }
            else
            {
                return new ServiceResponse(false, "Failed to delete category.");
            }
        }

        public async Task<List<GetCategory>> GetAllCategories()
        {
            var categories = await Repo.GetAllAsync();
            var mappedCategories = mapper.Map<List<GetCategory>>(categories);
            return mappedCategories;
        }

        public async Task<GetCategory> GetCategoryById(Guid id)
        {
            var category = await Repo.GetByIdAsync(id);
            if (category == null)
            {
                return null; 
            }
            var mappedCategory = mapper.Map<GetCategory>(category);
            return mappedCategory;
        }

        public async Task<GetCategory> GetCategoryByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            var categories = await Repo.GetAllAsync();
            var category = categories.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (category == null)
            {
                return null;
            }
            var mappedCategory = mapper.Map<GetCategory>(category);
            return mappedCategory;
        }

        public async Task<ServiceResponse> UpdateCategory(UpdateCategory category)
        {
            if (category == null)
            {
                return new ServiceResponse(false, "Category cannot be null.");
            }
            var mappedCategory = mapper.Map<Category>(category);
            var result = await Repo.UpdateAsync(mappedCategory);
            if (result != null)
            {
                return new ServiceResponse(true, "Category updated successfully.");
            }
            else
            {
                return new ServiceResponse(false, "Failed to update category.");
            }

        }
    }
}
