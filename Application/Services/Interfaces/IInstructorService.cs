using Application.DTOs.Learning;
using Application.DTOs.Responses;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IInstructorService
    {
        Task<ServiceResponse> GetMyCoursesAsync(string instructorId);
        Task<ServiceResponse> GetOverviewAsync(string instructorId);
        Task<ServiceResponse> GetSessionsAsync(string instructorId);
        Task<ServiceResponse> GetStudentsAsync(string instructorId);
        Task<ServiceResponse> GetSubmissionsAsync(string instructorId);
        Task<ServiceResponse> GetSubmissionAsync(Guid assignmentId, string studentId, string instructorId);
        Task<ServiceResponse> GradeSubmissionAsync(Guid assignmentId, string studentId, GradeSubmissionRequest request, string instructorId);
        Task<ServiceResponse> CreateSessionQrAsync(Guid sessionId, string userId, bool isAdminOrOrganizationAdmin);
        Task<ServiceResponse> GetSessionAttendanceAsync(Guid sessionId, string userId, bool isAdminOrOrganizationAdmin);
        Task<ServiceResponse> MarkAttendance(Guid sessionId, string userId);
        Task<IEnumerable<AttendanceRecord>> GetAttendanceRecords(Guid sessionId);
    }
}
