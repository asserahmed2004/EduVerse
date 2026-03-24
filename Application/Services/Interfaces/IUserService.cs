using Application.DTOs.Auth;

using Application.DTOs.Cloud;
using Application.DTOs.Course;
using Application.DTOs.Enrollments;
using Application.DTOs.Responses;
using Application.DTOs.Submission;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResponse>Enroll(Guid courseId,string userId);
        Task<ServiceResponse> AddCertificate(CreateCertificate certificate);
        
        Task<IEnumerable<GetCourse>> GetEnrolledCourses(string userId);
        Task<IEnumerable<GetUser>> GetEnrolledUsers(Guid courseId);
        Task<string> GetCertificateFile(Guid courseId, string userId);
        Task<IEnumerable<string>> GetUserCertificates(string userId);
        Task<Enrollment>GetEnrollmentData(Guid courseId,string userId);
        Task<ServiceResponse> UpdateProgress(Guid courseId, string userId, double progression);
        Task<ServiceResponse> SubmitAssignment(CreateAssignmentSubmission submission);
        
        Task<IEnumerable<GetAssignmentSubmission>> GetUserSubmissions(string Email);
        Task<IEnumerable<GetAssignmentSubmission>> GetAssignmentSubmissions(Guid Id);
        
        Task<GetAssignmentSubmission> GetSubmission(Guid Id,string Email);







    }
}
