using Application.DTOs.Auth;

using Application.DTOs.Cloud;
using Application.DTOs.Course;
using Application.DTOs.Enrollments;
using Application.DTOs.Payment;
using Application.DTOs.Responses;
using Application.DTOs.Submission;
using Application.DTOs.Learning;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        Task<IEnumerable<CertificateDto>> GetMyCertificates(string userId, string baseUrl);
        Task<ServiceResponse> GenerateCertificate(Guid courseId, string userId, string baseUrl);
        Task<ServiceResponse> VerifyCertificate(string code);
        Task<Enrollment>GetEnrollmentData(Guid courseId,string userId);
        Task<IEnumerable<Enrollment>> EnrollmentData();
        Task<ServiceResponse> UpdateProgress(Guid courseId, string userId, double progression);
        Task<CourseProgressDto?> GetCourseProgress(Guid courseId, string userId);
        Task<ServiceResponse> MarkSessionCompleted(Guid sessionId, string userId);
        Task<IEnumerable<StudentAssignmentDto>> GetMyAssignments(string userId);
        Task<ServiceResponse> SubmitAssignment(SubmitAssignmentRequest submission, string userId);
        Task<ServiceResponse> SubmitAssignment(CreateAssignmentSubmission submission);
        
        Task<IEnumerable<GetAssignmentSubmission>> GetUserSubmissions(string Email);
        Task<IEnumerable<GetAssignmentSubmission>> GetAssignmentSubmissions(Guid Id);
        
        Task<GetAssignmentSubmission> GetSubmission(Guid Id,string Email);
        Task <string> Payment(string userId, Guid Course,string Method);
        Task<IEnumerable<GetPayment>> GetUserPayments(string userId);
        Task<IEnumerable<GetPayment>> GetCoursePayments(Guid courseId);
        Task<GetPayment> GetPayment(Guid courseId, string userId);
        Task<ServiceResponse> UpdatePaymentFromCallback(JsonElement callbackData);
        Task<IEnumerable<NotificationDto>> GetMyNotifications(string userId);
        Task<ServiceResponse> MarkNotificationAsRead(Guid id, string userId);
        
        






    }
}
