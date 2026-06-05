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
        Task<ServiceResponse> CreateCourse(CreateCourse Course, string currentUserId, bool isAdmin);
        Task<ServiceResponse> UpdateCourse(UpdateCourse Course);
        Task<ServiceResponse> DeleteCourse(Guid id, string deletedById, string deletedByName);
        Task<ServiceResponse> RestoreCourse(Guid id, string restoredById, string restoredByName);
        Task<ServiceResponse> AssignInstructor(Guid courseId, string instructorId, string currentUserId, bool isAdmin);
        Task<bool> CourseExists(Guid id);
        Task<bool> IsCourseDeleted(Guid id);
        Task<bool> CanManageCourse(Guid courseId, string userId);
        Task<bool> CanManageSession(Guid sessionId, string userId);
        Task<bool> CanManageAssignment(Guid assignmentId, string userId);
        Task<List<GetCourse>> GetAllCourses(string? userid, bool isAdmin = false, bool isOrganizationAdmin = false, bool isInstructor = false);
        Task<List<GetCourse>> GetDeletedCourses(string? userid);
        Task <List<GetCourse>> Search(string name, string? userid, bool isAdmin = false, bool isOrganizationAdmin = false, bool isInstructor = false);
        Task<GetCourse> GetCourseById(Guid id, string? userid);
        Task<GetCourse> GetCourseByName(string name, string userid);
        Task<List<GetCourse>> GetCourseByCategory(Guid  categoryId, string? userid, bool isAdmin = false, bool isOrganizationAdmin = false, bool isInstructor = false);
        Task<AdminCourseDetailsDto?> GetAdminCourseDetails(Guid id, string? currentUserId, bool isAdmin, bool isOrganizationAdmin, bool isInstructor);

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
