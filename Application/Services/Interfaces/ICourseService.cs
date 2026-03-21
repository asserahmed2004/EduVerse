using Application.DTOs.Assignment;
using Application.DTOs.Course;
using Application.DTOs.Rating;
using Application.DTOs.Responses;
using Application.DTOs.Sessions;
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
        Task<ServiceResponse> AddSession(CreateSession session);
        Task<ServiceResponse> UpdateSession(UpdateSession session);
        Task<ServiceResponse> DeleteSession(Guid id);
        Task<List<GetSession>> GetCourseAllSessions(Guid courdeid);
        Task<GetSession> GetSessionById(Guid id);
        Task<GetSession> GetSessionByNumber(Guid courseid, int sessionnumber);
        Task<ServiceResponse> AddAssignment(CreateAssignment assignment);
        Task<ServiceResponse> UpdateAssignment(UpdateAssignment assignment);
        Task<ServiceResponse> DeleteAssignment(Guid id);
        Task<List<GetAssignment>> GetCourseAllAssignments(Guid courdeid);
        Task<GetAssignment> GetAssignmentById(Guid id);
        Task<GetAssignment> GetAssignmentBySession(Guid sessionid);






    }
}
