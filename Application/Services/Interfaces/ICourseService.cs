using Application.DTOs.Course;
using Application.DTOs.Rating;
using Application.DTOs.Responses;
using Domain.Entities;
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
        Task<List<GetCourse>> GetAllCourses(string? userid);
        Task<GetCourse> GetCourseById(Guid id, string? userid);
        Task<GetCourse> GetCourseByName(string name, string userid);
        Task<ServiceResponse> UpdateDuration(Guid id, int duration);
        Task<ServiceResponse> AddRating(CreateRating rating, string userid);
        


    }
}
