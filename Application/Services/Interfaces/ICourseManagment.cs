using Application.DTOs.Course;
using Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface ICourseService
    {
        Task<ServiceResponse> CreateCourse(CreateCourse Course);
        Task<ServiceResponse> UpdateCourse(UpdateCourse Course);
        Task<ServiceResponse> DeleteCourse(Guid id);
        Task<List<GetCourse>> GetAllCourses();
        Task<GetCourse> GetCourseById(Guid id);
        Task<GetCourse> GetCourseByName(string name);
        Task<ServiceResponse> IncrementDuration(Guid id, int duration);

    }
}
