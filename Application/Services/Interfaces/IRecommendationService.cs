using Application.DTOs.Responses;
using System;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface IRecommendationService
    {
        Task<ServiceResponse> GetPersonalizedRecommendationsAsync(string studentId);
        Task<ServiceResponse> GetSimilarCoursesAsync(Guid courseId);
        Task<ServiceResponse> GetTrendingCoursesAsync();
    }
}
