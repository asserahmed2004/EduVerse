using Application.DTOs.Auth;

using Application.DTOs.Cloud;
using Application.Configuration;
using Application.DTOs.Course;
using Application.DTOs.Enrollments;
using Application.DTOs.Learning;
using Application.DTOs.Payment;
using Application.DTOs.Responses;
using Application.DTOs.Submission;
using Application.Exceptions;
using Application.Services;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Application.Services.Implementitions
{
    public class UserService(IGeneric<Enrollment> Enrollment, ICloudService cloud
        , IGeneric<Course> Courses, IUserManagment userManagment, IGeneric<AssignmentSubmission> AssignmentSubmission,
        IGeneric<Payment> Payments, IMapper mapper, IHttpClientFactory httpClientFactory,
        IGeneric<Session> Sessions, IGeneric<Assignment> Assignments,
        IGeneric<StudentSessionProgress> Progresses, IGeneric<CertificateRecord> Certificates,
        IGeneric<Notification> Notifications, IGeneric<AttendanceRecord> AttendanceRecords,
        IGeneric<SessionMaterial> SessionMaterials, IGeneric<Organization> Organizations,
        IOptions<PaymobOptions> paymobOptions, ILogger<UserService> logger) : IUserService
    {
        private static readonly SemaphoreSlim CertificateGenerationLock = new(1, 1);
        private static readonly HashSet<string> PaymobSensitiveFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "token",
            "api_key",
            "client_secret",
            "payment_token",
            "secret",
            "email",
            "phone_number",
            "first_name",
            "last_name",
            "pan"
        };
        private readonly PaymobOptions paymob = paymobOptions.Value;
        private readonly HttpClient paymobClient = httpClientFactory.CreateClient("Paymob");
        public async Task<ServiceResponse> AddCertificate(CreateCertificate certificate)
        {
            if (certificate == null || string.IsNullOrEmpty(certificate.Email) || certificate.CourseId == Guid.Empty)
            {
                return new ServiceResponse(false, "Invalid certificate data.");
            }
            var user = await userManagment.GetUserByEmail(certificate.Email);
            var userId = user.Id.ToString();
            var existingEnrollments = await Enrollment.GetAllAsync();
            var existingEnrollment = existingEnrollments.FirstOrDefault(e => e.CourseId == certificate.CourseId && e.StudentId == userId);
            if (existingEnrollment == null)
            {
                return new ServiceResponse(false, "User is not enrolled in the course.");
            }
            var fileDetails = new FileDetails
            {
                FileName = $"{userId}_{certificate.CourseId}_Certificate.pdf",
                Folder = "certificates",

            };
            var file = new AddCloudFile
            {
                File = certificate.CertificateFile,
                Details = fileDetails
            };

            var uploadResult = await cloud.UploadFileAsync(file);
            if (!uploadResult.success)
            {
                return new ServiceResponse(false, "Certificate upload failed.");
            }
            var newenrollment = new Enrollment
            {
                CourseId = certificate.CourseId,
                StudentId = userId,
                FileUrl = fileDetails.FileName,
                GraduationDate = DateTime.Now,
                Progression = existingEnrollment.Progression,
                EnrollmentDate = existingEnrollment.EnrollmentDate


            };
            var updateResult = await Enrollment.UpdateAsync(newenrollment);
            if (updateResult != null)
            {
                return new ServiceResponse(true, "Certificate added successfully.");
            }
            else
            {
                return new ServiceResponse(false, "Failed to update enrollment with certificate.");
            }

        }

        public async Task<ServiceResponse> Enroll(Guid courseId, string userId)
        {
            if (courseId == Guid.Empty || string.IsNullOrEmpty(userId))
            {
                return new ServiceResponse(false, "Invalid course ID or user ID.");
            }
            var course = await Courses.GetByIdAsync(courseId);
            if (course == null || course.IsDeleted)
            {
                return new ServiceResponse(false, "Course not found.");
            }
            var existingEnrollment = (await Enrollment.GetAllAsync()).FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);
            if (existingEnrollment != null)
            {
                return new ServiceResponse(false, "Student is already enrolled in this course.");
            }

            
            var enrollment = new Enrollment
            {
                CourseId = courseId,
                StudentId = userId,
                EnrollmentDate = DateTime.Now,
                Progression = 0,
                ProgressPercentage = 0
            };
            var result = await Enrollment.AddAsync(enrollment);
            if (result != null)
            {
                await CreateNotification(userId, "Course enrolled", $"You are enrolled in {course.Name}.");
                return new ServiceResponse(true, "Enrollment successful.");
            }
            else
            {
                return new ServiceResponse(false, "Enrollment failed.");
            }


        }


        public async Task<string> GetCertificateFile(Guid courseId, string Email)
        {
            var user = await userManagment.GetUserByEmail(Email);
            var userId = user.Id.ToString();
            var enrollment = await Enrollment.Query().FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null || string.IsNullOrEmpty(enrollment.FileUrl))
            {
                return null;
            }
            var fileUrl = enrollment.FileUrl;
            return fileUrl;
        }

        public async Task<IEnumerable<GetCourse>> GetEnrolledCourses(string userId)
        {
            var enrollments = await Enrollment.Query().Where(e => e.StudentId == userId).ToListAsync();
            var enrollmentByCourseId = enrollments.GroupBy(e => e.CourseId).ToDictionary(group => group.Key, group => group.First());
            var courseIds = enrollments.Select(e => e.CourseId).ToList();
            var courses = await Courses.Query().Where(c => !c.IsDeleted && courseIds.Contains(c.Id)).ToListAsync();
            var sessions = await Sessions.Query().Where(s => courseIds.Contains(s.CourseId)).ToListAsync();
            var progressRows = await Progresses.Query().Where(p => p.StudentId == userId && courseIds.Contains(p.CourseId)).ToListAsync();
            var trainerIds = courses.Select(c => c.InstructorId)
                .Concat(sessions.Select(s => s.TrainerId))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();
            var trainers = await userManagment.QueryUsers()
                .Where(u => trainerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
            var enrolledCourses = mapper.Map<List<GetCourse>>(courses);

            foreach (var course in enrolledCourses)
            {
                var courseSessions = sessions.Where(s => s.CourseId == course.Id).ToList();
                var sessionIds = courseSessions.Select(s => s.Id).ToHashSet();
                var doneSessions = progressRows.Count(p => p.IsDone && p.CourseId == course.Id && sessionIds.Contains(p.SessionId));
                var progressPercent = CalculatePercentage(doneSessions, courseSessions.Count);
                course.ProgressPercent = progressPercent;

                var trainerId = courses.FirstOrDefault(c => c.Id == course.Id)?.InstructorId
                    ?? courseSessions.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.TrainerId))?.TrainerId;
                course.InstructorId = trainerId;
                if (!string.IsNullOrWhiteSpace(trainerId) && trainers.TryGetValue(trainerId, out var trainer))
                {
                    course.InstructorName = trainer?.FullName ?? trainer?.UserName ?? trainer?.Email;
                }
            }
            return enrolledCourses;
        }

        public async Task<IEnumerable<GetUser>> GetEnrolledUsers(Guid courseId)
        {
            var enrollments = await Enrollment.Query().Where(e => e.CourseId == courseId).ToListAsync();
            var userIds = enrollments.Select(e => e.StudentId).ToList();
            var users = await userManagment.QueryUsers().Where(u => userIds.Contains(u.Id)).ToListAsync();
            var enrolledUsers = mapper.Map<IEnumerable<GetUser>>(users);
            return enrolledUsers;
        }

        public async Task<Enrollment> GetEnrollmentData(Guid courseId, string Email)
        {
            var user = await userManagment.GetUserByEmail(Email);
            var userId = user.Id.ToString();
            var enrollment = await Enrollment.Query().FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);
            return enrollment;
        }
        public async Task<IEnumerable<Enrollment>> EnrollmentData()
        {
            var enrollments = await Enrollment.GetAllAsync();
            return enrollments;
        }


        public async Task<IEnumerable<string>> GetUserCertificates(string Email)
        {
            var user = await userManagment.GetUserByEmail(Email);
            var userId = user.Id.ToString();

            var enrollments = await Enrollment.Query().Where(e => e.StudentId == userId && !string.IsNullOrEmpty(e.FileUrl)).ToListAsync();
            var certificateUrls = enrollments.Select(e => e.FileUrl).ToList();
            return certificateUrls;
        }

        public async Task<IEnumerable<CertificateDto>> GetMyCertificates(string userId, string baseUrl)
        {
            var certificates = await Certificates.Query().Where(c => c.StudentId == userId).OrderByDescending(c => c.IssuedAt).ToListAsync();
            var courseIds = certificates.Select(c => c.CourseId).Distinct().ToList();
            var courses = await Courses.Query().Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
            var organizationIds = courses.Values
                .Where(course => course.OrganizationId.HasValue)
                .Select(course => course.OrganizationId!.Value)
                .Distinct()
                .ToList();
            var organizations = await Organizations.Query()
                .Where(organization => organizationIds.Contains(organization.Id))
                .ToDictionaryAsync(organization => organization.Id);
            var user = await userManagment.GetUserById(userId);
            return certificates.Select(c => new CertificateDto
            {
                Id = c.Id,
                CourseId = c.CourseId,
                CourseName = courses.TryGetValue(c.CourseId, out var course) ? course.Name : "Course",
                StudentName = user?.FullName ?? user?.Email ?? "Student",
                OrganizationName = course?.OrganizationId is Guid organizationId
                    && organizations.TryGetValue(organizationId, out var organization)
                        ? organization.Name
                        : "EduVerse",
                CertificateCode = c.CertificateCode,
                IssuedAt = c.IssuedAt,
                FileUrl = BuildCertificateDownloadUrl(baseUrl, c.Id),
                DownloadUrl = BuildCertificateDownloadUrl(baseUrl, c.Id),
                Status = c.Status,
                VerificationUrl = $"{baseUrl.TrimEnd('/')}/Certificate/Verify/{Uri.EscapeDataString(c.CertificateCode)}"
            });
        }

        public async Task<ServiceResponse> GenerateCertificate(Guid courseId, string userId, string baseUrl)
        {
            var enrollment = await Enrollment.Query().FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null)
                return new ServiceResponse(false, "Enrollment not found.");

            await CertificateGenerationLock.WaitAsync();
            try
            {
                var existing = await Certificates.Query(true)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.StudentId == userId);
                if (existing != null)
                {
                    var expectedFileUrl = BuildCertificateDownloadPath(existing.Id);
                    if (!string.Equals(existing.FileUrl, expectedFileUrl, StringComparison.Ordinal))
                    {
                        existing.FileUrl = expectedFileUrl;
                        await Certificates.UpdateAsync(existing);
                    }

                    return new ServiceResponse(
                        true,
                        "Certificate already exists.",
                        (await GetMyCertificates(userId, baseUrl)).FirstOrDefault(c => c.CourseId == courseId));
                }

                var eligibility = await GetCertificateEligibility(courseId, userId);
                if (eligibility == null)
                    return new ServiceResponse(false, "Certificate eligibility could not be checked.");

                if (!eligibility.CanReceiveCertificate)
                    return new ServiceResponse(false, eligibility.Message);

                var code = $"EDU-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
                var certificate = new CertificateRecord
                {
                    StudentId = userId,
                    CourseId = courseId,
                    CertificateCode = code,
                    IssuedAt = DateTime.UtcNow,
                    Status = "Valid"
                };
                certificate.FileUrl = BuildCertificateDownloadPath(certificate.Id);
                await Certificates.AddAsync(certificate);
                enrollment.CertificateCode = code;
                enrollment.GraduationDate ??= DateTime.UtcNow;
                await Enrollment.UpdateAsync(enrollment);
                await CreateNotification(userId, "Certificate issued", "Your course certificate is ready.");
                return new ServiceResponse(true, "Certificate generated successfully.", (await GetMyCertificates(userId, baseUrl)).FirstOrDefault(c => c.CourseId == courseId));
            }
            finally
            {
                CertificateGenerationLock.Release();
            }
        }

        public async Task<CertificateDownloadDto?> GetCertificateDownload(Guid certificateId, string userId)
        {
            if (certificateId == Guid.Empty || string.IsNullOrWhiteSpace(userId))
                return null;

            var certificate = await Certificates.Query()
                .FirstOrDefaultAsync(c => c.Id == certificateId && c.StudentId == userId && c.Status == "Valid");
            if (certificate == null)
                return null;

            var course = await Courses.Query().FirstOrDefaultAsync(c => c.Id == certificate.CourseId);
            var student = await userManagment.GetUserById(userId);
            if (course == null || student == null)
                return null;

            Organization? organization = null;
            if (course.OrganizationId.HasValue)
                organization = await Organizations.Query().FirstOrDefaultAsync(o => o.Id == course.OrganizationId.Value);

            var studentName = student.FullName ?? student.UserName ?? student.Email ?? "Student";
            var courseName = string.IsNullOrWhiteSpace(course.Name) ? course.Title : course.Name;
            var organizationName = organization?.Name ?? "EduVerse";
            var content = CertificatePdfGenerator.Generate(
                studentName,
                courseName,
                organizationName,
                certificate.IssuedAt,
                certificate.CertificateCode);

            return new CertificateDownloadDto
            {
                Content = content,
                FileName = $"EduVerse-Certificate-{SafeFileName(courseName)}-{SafeFileName(studentName)}.pdf"
            };
        }

        public async Task<ServiceResponse> VerifyCertificate(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new ServiceResponse(false, "Certificate code is required");

            var certificate = (await Certificates.GetAllAsync()).FirstOrDefault(c => string.Equals(c.CertificateCode, code, StringComparison.OrdinalIgnoreCase));
            if (certificate == null)
                return new ServiceResponse(false, "Certificate not found or invalid");

            var user = await userManagment.GetUserById(certificate.StudentId);
            var course = await Courses.GetByIdAsync(certificate.CourseId);
            return new ServiceResponse(true, "Certificate is valid", new CertificateVerificationDto
            {
                CertificateCode = certificate.CertificateCode,
                StudentName = user?.FullName ?? user?.Email ?? "Student",
                CourseName = course?.Name ?? "Course",
                IssueDate = certificate.IssuedAt,
                Status = certificate.Status
            });
        }



        public async Task<ServiceResponse> UpdateProgress(Guid courseId, string Email, double progression)
        {
            var user = await userManagment.GetUserByEmail(Email);
            var userId = user.Id.ToString();
            var existingEnrollment = (await Enrollment.GetAllAsync()).FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);
            if (existingEnrollment == null)
            {
                return new ServiceResponse(false, "Enrollment not found.");
            }
            existingEnrollment.Progression += progression;
            if (existingEnrollment.Progression >= 100)
            {
                existingEnrollment.Progression = 100;
                MarkEnrollmentCompleted(existingEnrollment);

            }
            existingEnrollment.ProgressPercentage = existingEnrollment.Progression;
            var updateResult = await Enrollment.UpdateAsync(existingEnrollment);
            if (updateResult != null)
            {
                return new ServiceResponse(true, "Progress updated successfully.");
            }
            else
            {
                return new ServiceResponse(false, "Failed to update progress.");



            }
        }

        public async Task<CourseProgressDto?> GetCourseProgress(Guid courseId, string userId)
        {
            var enrollment = (await Enrollment.GetAllAsync()).FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null)
                return null;

            var course = await Courses.GetByIdAsync(courseId);
            if (course == null || course.IsDeleted)
                return null;

            var sessions = await Sessions.Query().Where(s => s.CourseId == courseId).OrderBy(s => s.SessionNumber).ToListAsync();
            var progressRows = await Progresses.Query().Where(p => p.CourseId == courseId && p.StudentId == userId).ToListAsync();
            var sessionIds = sessions.Select(s => s.Id).ToList();
            var assignments = await Assignments.Query().Where(a => sessionIds.Contains(a.SessionId)).ToListAsync();
            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var submissions = await AssignmentSubmission.Query().Where(s => s.StudentId == userId && assignmentIds.Contains(s.AssignmentId)).ToListAsync();
            var materials = await SessionMaterials.Query().Where(m => sessionIds.Contains(m.SessionId)).ToListAsync();
            var totalSessions = sessions.Count;
            var doneSessions = progressRows.Count(p => p.IsDone && sessionIds.Contains(p.SessionId));
            var progressPercentage = CalculatePercentage(doneSessions, totalSessions);
            var completedAt = totalSessions > 0 && doneSessions == totalSessions
                ? progressRows
                    .Where(p => p.IsDone && sessionIds.Contains(p.SessionId))
                    .Select(p => p.DoneAt)
                    .Where(doneAt => doneAt.HasValue)
                    .DefaultIfEmpty(DateTime.UtcNow)
                    .Max()
                : null;

            return new CourseProgressDto
            {
                CourseId = courseId,
                CourseName = course.Name,
                TotalSessions = totalSessions,
                DoneSessions = doneSessions,
                ProgressPercentage = progressPercentage,
                IsCompleted = totalSessions > 0 && doneSessions == totalSessions,
                CompletedAt = completedAt,
                Sessions = sessions.Select(session =>
                {
                    var progress = progressRows.FirstOrDefault(p => p.SessionId == session.Id);
                    var isDone = progress?.IsDone ?? false;
                    return new SessionProgressDto
                    {
                        SessionId = session.Id,
                        CourseId = session.CourseId,
                        Title = session.Title,
                        SessionNumber = session.SessionNumber,
                        FileUrl = session.FileUrl,
                        Description = session.Description,
                        VideoUrl = session.VideoUrl,
                        ExternalLink = session.ExternalLink,
                        IsDone = isDone,
                        DoneAt = progress?.DoneAt,
                        IsCompleted = isDone,
                        CompletedAt = progress?.DoneAt,
                        Materials = materials.Where(m => m.SessionId == session.Id).Select(ToMaterialDto).ToList(),
                        Assignments = assignments.Where(a => a.SessionId == session.Id).Select(a => BuildStudentAssignmentDto(a, course, session, submissions.FirstOrDefault(s => s.AssignmentId == a.Id))).ToList()
                    };
                }).ToList()
            };
        }

        public async Task<ServiceResponse> ToggleSessionDone(Guid sessionId, string userId)
        {
            var session = await Sessions.GetByIdAsync(sessionId);
            if (session == null)
                return new ServiceResponse(false, "Session not found.");

            var course = await Courses.GetByIdAsync(session.CourseId);
            if (course == null || course.IsDeleted)
                return new ServiceResponse(false, "Course not found or unavailable.");

            var enrollment = (await Enrollment.GetAllAsync()).FirstOrDefault(e => e.CourseId == session.CourseId && e.StudentId == userId);
            if (enrollment == null)
                return new ServiceResponse(false, "You must enroll in this course before updating personal progress.");

            var now = DateTime.UtcNow;

            var existing = (await Progresses.GetAllAsync()).FirstOrDefault(p => p.StudentId == userId && p.SessionId == sessionId);
            if (existing == null)
            {
                await Progresses.AddAsync(new StudentSessionProgress
                {
                    StudentId = userId,
                    CourseId = session.CourseId,
                    SessionId = sessionId,
                    IsDone = true,
                    DoneAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                existing.IsDone = !existing.IsDone;
                existing.DoneAt = existing.IsDone ? now : null;
                existing.CourseId = session.CourseId;
                existing.UpdatedAt = now;
                await Progresses.UpdateAsync(existing);
            }

            var allCourseSessions = (await Sessions.GetAllAsync()).Where(s => s.CourseId == session.CourseId).Select(s => s.Id).ToHashSet();
            var progressRows = (await Progresses.GetAllAsync()).Where(p => p.CourseId == session.CourseId && p.StudentId == userId).ToList();
            var updatedProgress = progressRows.FirstOrDefault(p => p.SessionId == sessionId);
            var summary = await SyncEnrollmentProgress(session.CourseId, userId, enrollment, allCourseSessions);

            var data = new ToggleSessionDoneResultDto
            {
                SessionId = sessionId,
                CourseId = session.CourseId,
                IsDone = updatedProgress?.IsDone ?? true,
                DoneAt = updatedProgress?.DoneAt,
                DoneSessions = summary.DoneSessions,
                TotalSessions = summary.TotalSessions,
                ProgressPercentage = summary.ProgressPercentage
            };

            return new ServiceResponse(true, data.IsDone ? "Session marked as done." : "Session marked as not done.", data);
        }

        public async Task<AssignmentProgressDto?> GetAssignmentProgress(Guid courseId, string userId)
        {
            var enrollment = await Enrollment.Query().FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null)
                return null;

            var course = await Courses.Query().FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null || course.IsDeleted)
                return null;

            var sessionIds = await Sessions.Query()
                .Where(s => s.CourseId == courseId)
                .Select(s => s.Id)
                .ToListAsync();
            var assignmentIds = await Assignments.Query()
                .Where(a => sessionIds.Contains(a.SessionId))
                .Select(a => a.Id)
                .ToListAsync();
            var totalAssignments = assignmentIds.Count;
            var submittedAssignments = totalAssignments == 0
                ? 0
                : await AssignmentSubmission.Query()
                    .Where(s => s.StudentId == userId && assignmentIds.Contains(s.AssignmentId))
                    .Select(s => s.AssignmentId)
                    .Distinct()
                    .CountAsync();
            var percentage = totalAssignments == 0 ? 100 : CalculatePercentage(submittedAssignments, totalAssignments);

            return new AssignmentProgressDto
            {
                CourseId = courseId,
                TotalAssignments = totalAssignments,
                SubmittedAssignments = submittedAssignments,
                AssignmentProgressPercentage = percentage,
                RequiredPercentage = 80,
                HasRequiredAssignmentProgress = percentage >= 80
            };
        }

        public async Task<CertificateEligibilityDto?> GetCertificateEligibility(Guid courseId, string userId)
        {
            var enrollment = await Enrollment.Query().FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null)
                return null;

            var course = await Courses.Query().FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null || course.IsDeleted)
                return null;

            var assignmentProgress = await GetAssignmentProgress(courseId, userId);
            if (assignmentProgress == null)
                return null;

            var isCourseCompleted = IsEnrollmentCompleted(enrollment);
            var isDurationFinished = IsCourseDurationFinished(course, enrollment);
            var canReceiveCertificate = isCourseCompleted
                && assignmentProgress.HasRequiredAssignmentProgress
                && isDurationFinished;
            var message = canReceiveCertificate
                ? "You are eligible to receive the certificate."
                : !isCourseCompleted
                    ? "Complete the course with 100% progress to unlock your certificate."
                    : !assignmentProgress.HasRequiredAssignmentProgress
                    ? "You need to submit at least 80% of assignments to receive the certificate."
                    : "You have completed the required assignments, but the certificate will be available after the course duration ends.";

            return new CertificateEligibilityDto
            {
                CourseId = courseId,
                AssignmentProgressPercentage = assignmentProgress.AssignmentProgressPercentage,
                RequiredPercentage = assignmentProgress.RequiredPercentage,
                HasRequiredAssignmentProgress = assignmentProgress.HasRequiredAssignmentProgress,
                IsCourseCompleted = isCourseCompleted,
                IsCourseDurationFinished = isDurationFinished,
                CanReceiveCertificate = canReceiveCertificate,
                Message = message
            };
        }

        public async Task<ServiceResponse> MarkSessionCompleted(Guid sessionId, string userId)
        {
            if (sessionId == Guid.Empty)
                return new ServiceResponse(false, "Invalid session id.");
            if (string.IsNullOrWhiteSpace(userId))
                return new ServiceResponse(false, "Student id is required.");

            var session = await Sessions.GetByIdAsync(sessionId);
            if (session == null)
                return new ServiceResponse(false, "Session not found.");

            var course = await Courses.GetByIdAsync(session.CourseId);
            if (course == null || course.IsDeleted)
                return new ServiceResponse(false, "Course not found or unavailable.");

            var enrollment = (await Enrollment.GetAllAsync()).FirstOrDefault(e => e.CourseId == session.CourseId && e.StudentId == userId);
            if (enrollment == null)
                return new ServiceResponse(false, "You must enroll in this course before completing sessions.");

            var now = DateTime.UtcNow;
            var existing = (await Progresses.GetAllAsync()).FirstOrDefault(p => p.StudentId == userId && p.SessionId == sessionId);
            var wasAlreadyCompleted = existing?.IsDone == true;

            if (existing == null)
            {
                await Progresses.AddAsync(new StudentSessionProgress
                {
                    StudentId = userId,
                    CourseId = session.CourseId,
                    SessionId = sessionId,
                    IsDone = true,
                    DoneAt = now,
                    UpdatedAt = now
                });
            }
            else if (!existing.IsDone || existing.CourseId != session.CourseId)
            {
                existing.IsDone = true;
                existing.DoneAt ??= now;
                existing.CourseId = session.CourseId;
                existing.UpdatedAt = now;
                await Progresses.UpdateAsync(existing);
            }

            await SyncEnrollmentProgress(session.CourseId, userId, enrollment);
            var courseProgress = await GetCourseProgress(session.CourseId, userId);

            return new ServiceResponse(
                true,
                wasAlreadyCompleted ? "Session was already completed." : "Session marked as completed.",
                courseProgress);
        }

        public async Task<IEnumerable<StudentAssignmentDto>> GetMyAssignments(string userId)
        {
            var enrollments = await Enrollment.Query().Where(e => e.StudentId == userId).ToListAsync();
            var courseIds = enrollments.Select(e => e.CourseId).ToList();
            var courses = await Courses.Query().Where(c => !c.IsDeleted && courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
            var activeCourseIds = courses.Keys.ToList();
            var sessions = await Sessions.Query().Where(s => activeCourseIds.Contains(s.CourseId)).ToListAsync();
            var sessionById = sessions.ToDictionary(s => s.Id, s => s);
            var sessionIds = sessionById.Keys.ToList();
            var assignments = await Assignments.Query().Where(a => sessionIds.Contains(a.SessionId)).ToListAsync();
            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var submissions = await AssignmentSubmission.Query().Where(s => s.StudentId == userId && assignmentIds.Contains(s.AssignmentId)).ToListAsync();

            return assignments.Select(assignment =>
            {
                var session = sessionById[assignment.SessionId];
                var course = courses.TryGetValue(session.CourseId, out var foundCourse) ? foundCourse : new Course { Id = session.CourseId, Name = "Course" };
                return BuildStudentAssignmentDto(assignment, course, session, submissions.FirstOrDefault(s => s.AssignmentId == assignment.Id));
            }).OrderBy(a => a.DueDate ?? DateTime.MaxValue).ToList();
        }

        public async Task<ServiceResponse> SubmitAssignment(SubmitAssignmentRequest submission, string userId)
        {
            if (submission == null || submission.AssignmentId == Guid.Empty)
                return new ServiceResponse(false, "Invalid submission data.");
            if (string.IsNullOrWhiteSpace(userId))
                return new ServiceResponse(false, "Student id is required.");

            var textAnswer = string.IsNullOrWhiteSpace(submission.TextAnswer) ? null : submission.TextAnswer.Trim();
            var file = submission.File?.Length > 0 ? submission.File : null;
            if (textAnswer == null && file == null)
                return new ServiceResponse(false, "Add a text answer or upload a file before submitting.");

            var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
            if (assignment == null)
                return new ServiceResponse(false, "Assignment not found.");

            var session = await Sessions.GetByIdAsync(assignment.SessionId);
            if (session == null)
                return new ServiceResponse(false, "Session not found.");

            var enrolled = await Enrollment.Query().AnyAsync(e => e.CourseId == session.CourseId && e.StudentId == userId);
            if (!enrolled)
                return new ServiceResponse(false, "You can submit assignments only for enrolled courses.");

            var create = new CreateAssignmentSubmission
            {
                AssignmentId = submission.AssignmentId,
                StudentId = userId,
                File = file,
                TextAnswer = textAnswer
            };

            return await SubmitAssignment(create);
        }
        public async Task<IEnumerable<GetAssignmentSubmission>> GetAssignmentSubmissions(Guid Id)
        {
            if (Id == Guid.Empty)
            {
                return null;
            }
            var submissions = await AssignmentSubmission.Query().Where(s => s.AssignmentId == Id).ToListAsync();
            if (submissions == null || submissions.Count == 0)
            {
                return null;
            }
            var mappedSubmissions = mapper.Map<IEnumerable<GetAssignmentSubmission>>(submissions);
            return mappedSubmissions;
        }

        public async Task<IEnumerable<GetPayment>> GetUserPayments(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return [];
            }

            var payments = await Payments.Query()
                .Where(p => p.StudentId == userId)
                .OrderByDescending(p => p.SubmittingDate)
                .ToListAsync();

            return mapper.Map<IEnumerable<GetPayment>>(payments);
        }

        public async Task<IEnumerable<GetPayment>> GetCoursePayments(Guid courseId)
        {
            if (courseId == Guid.Empty)
            {
                return [];
            }

            var payments = await Payments.Query()
                .Where(p => p.CourseId == courseId)
                .OrderByDescending(p => p.SubmittingDate)
                .ToListAsync();

            return mapper.Map<IEnumerable<GetPayment>>(payments);
        }

        public async Task<GetPayment> GetPayment(Guid courseId, string userId)
        {
            if (courseId == Guid.Empty || string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var payment = await Payments.Query()
                .FirstOrDefaultAsync(p => p.CourseId == courseId && p.StudentId == userId);

            return payment == null ? null : mapper.Map<GetPayment>(payment);
        }

        public async Task<IEnumerable<GetAssignmentSubmission>> GetUserSubmissions(string Email)
        {
            if (string.IsNullOrEmpty(Email))
            {
                return null;
            }
            var user = await userManagment.GetUserByEmail(Email);
            var userId = user.Id.ToString();
            var submissions = await AssignmentSubmission.Query().Where(s => s.StudentId == userId).ToListAsync();
            if (submissions == null || submissions.Count == 0)
            {
                return null;
            }
            var mappedSubmissions = mapper.Map<IEnumerable<GetAssignmentSubmission>>(submissions);
            return mappedSubmissions;
        }

        public async Task<ServiceResponse> SubmitAssignment(CreateAssignmentSubmission submission)
        {
            if (submission == null || submission.AssignmentId == Guid.Empty || string.IsNullOrWhiteSpace(submission.StudentId))
                return new ServiceResponse(false, "Invalid submission data.");

            var textAnswer = string.IsNullOrWhiteSpace(submission.TextAnswer) ? null : submission.TextAnswer.Trim();
            var submittedFile = submission.File?.Length > 0 ? submission.File : null;
            if (textAnswer == null && submittedFile == null)
                return new ServiceResponse(false, "Add a text answer or upload a file before submitting.");

            var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
            if (assignment == null)
                return new ServiceResponse(false, "Assignment not found.");

            var existingSubmission = (await AssignmentSubmission.GetAllAsync()).FirstOrDefault(s => s.AssignmentId == submission.AssignmentId && s.StudentId == submission.StudentId);
            if (existingSubmission != null)
            {
                var previousFileName = existingSubmission.FileUrl;
                string? uploadedFileName = null;
                if (submittedFile != null)
                {
                    var fileDetails = new FileDetails
                    {
                        FileName = $"{submission.AssignmentId}_{Guid.NewGuid():N}_Submission{Path.GetExtension(submittedFile.FileName)}",
                        Folder = "submissions"
                    };
                    var file = new AddCloudFile
                    {
                        File = submittedFile,
                        Details = fileDetails
                    };
                    var uploadResult = await cloud.UploadFileAsync(file);
                    if (!uploadResult.success)
                        return new ServiceResponse(false, "File upload failed.");

                    uploadedFileName = fileDetails.FileName;
                    existingSubmission.FileUrl = fileDetails.FileName;
                }

                existingSubmission.TextAnswer = textAnswer ?? existingSubmission.TextAnswer;
                existingSubmission.SubmittedAt = DateTime.UtcNow;
                existingSubmission.IsLate = assignment.DueDate.HasValue && DateTime.UtcNow > assignment.DueDate.Value;
                existingSubmission.Grade = null;
                existingSubmission.Feedback = null;

                try
                {
                    var updateResult = await AssignmentSubmission.UpdateAsync(existingSubmission);
                    if (updateResult == null)
                    {
                        await DeleteUploadedSubmissionFile(uploadedFileName);
                        return new ServiceResponse(false, "Failed to update assignment submission.");
                    }
                }
                catch
                {
                    await DeleteUploadedSubmissionFile(uploadedFileName);
                    return new ServiceResponse(false, "Failed to update assignment submission.");
                }

                if (!string.IsNullOrWhiteSpace(uploadedFileName) && previousFileName != uploadedFileName)
                    await DeleteUploadedSubmissionFile(previousFileName);
                return new ServiceResponse(true, "Assignment submission updated successfully.");
            }
            else
            {
                var mapped = mapper.Map<AssignmentSubmission>(submission);
                string? uploadedFileName = null;
                if (submittedFile != null)
                {
                    var fileDetails = new FileDetails
                    {
                        FileName = $"{submission.AssignmentId}_{Guid.NewGuid():N}_Submission{Path.GetExtension(submittedFile.FileName)}",
                        Folder = "submissions"
                    };
                    var file = new AddCloudFile
                    {
                        File = submittedFile,
                        Details = fileDetails
                    };
                    var uploadResult = await cloud.UploadFileAsync(file);
                    if (!uploadResult.success)
                    {
                        return new ServiceResponse(false, "File upload failed.");
                    }
                    mapped.FileUrl = fileDetails.FileName;
                    uploadedFileName = fileDetails.FileName;
                }
                else
                {
                    mapped.FileUrl = string.Empty;
                }
                mapped.TextAnswer = textAnswer;
                mapped.SubmittedAt = DateTime.UtcNow;
                mapped.IsLate = assignment.DueDate.HasValue && DateTime.UtcNow > assignment.DueDate.Value;

                try
                {
                    var result = await AssignmentSubmission.AddAsync(mapped);
                    if (result == null)
                    {
                        await DeleteUploadedSubmissionFile(uploadedFileName);
                        return new ServiceResponse(false, "Failed to submit assignment.");
                    }
                    return new ServiceResponse(true, "Assignment submitted successfully.");
                }
                catch
                {
                    await DeleteUploadedSubmissionFile(uploadedFileName);
                    return new ServiceResponse(false, "Failed to submit assignment.");
                }
            }
        }

        private async Task DeleteUploadedSubmissionFile(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            try
            {
                await cloud.DeleteFileAsync(new FileDetails { FileName = fileName, Folder = "submissions" });
            }
            catch
            {
                // File cleanup should not replace the submission persistence result.
            }
        }

        public async Task<GetAssignmentSubmission> GetSubmission(Guid Id, string Email)
        {
            if (Id == Guid.Empty || string.IsNullOrEmpty(Email))
            {
                return null;
            }
            var user = await userManagment.GetUserByEmail(Email);
            var userId = user.Id.ToString();
            var submission = await AssignmentSubmission.Query().FirstOrDefaultAsync(s => s.AssignmentId == Id && s.StudentId == userId);
            if (submission == null)
            {
                return null;
            }
            var mappedSubmission = mapper.Map<GetAssignmentSubmission>(submission);
            return mappedSubmission;
        }
        public async Task<string> Payment(string userId, Guid Course, string Method)
        {
            var course = await Courses.GetByIdAsync(Course);
            var user = await userManagment.GetUserById(userId);
            if (course == null || course.IsDeleted || user == null)
            {
                return null;
            }

            var isAlreadyEnrolled = await Enrollment.Query()
                .AnyAsync(item => item.CourseId == Course && item.StudentId == userId);
            if (isAlreadyEnrolled)
            {
                throw new PaymobException(
                    "The student is already enrolled in this course.",
                    "You are already enrolled in this course.",
                    (int)HttpStatusCode.Conflict);
            }

            if (course.Price <= 0)
            {
                var enrollResult = await Enroll(Course, userId);
                return enrollResult.success ? "Free enrollment completed." : null;
            }

            var normalizedMethod = NormalizePaymentMethod(Method);
            var integrationId = GetIntegrationId(normalizedMethod);
            ValidatePaymobConfiguration();

            var amountCents = checked((long)Math.Round(
                course.Price * 100,
                MidpointRounding.AwayFromZero));
            if (amountCents <= 0)
            {
                throw new PaymobException(
                    "The calculated PayMob amount is invalid.",
                    "The course price is invalid.",
                    (int)HttpStatusCode.BadRequest);
            }

            var specialReference = RandomNumberGenerator.GetInt32(100_000_000, 1_000_000_000).ToString();
            var merchantOrderId = $"eduverse-{Guid.NewGuid():N}";
            var (firstName, lastName) = SplitFullName(user.FullName);
            var billingData = new
            {
                apartment = "NA",
                email = string.IsNullOrWhiteSpace(user.Email) ? "NA" : user.Email,
                floor = "NA",
                first_name = firstName,
                street = "NA",
                building = "NA",
                phone_number = string.IsNullOrWhiteSpace(user.PhoneNumber) ? "NA" : user.PhoneNumber,
                shipping_method = "NA",
                postal_code = "NA",
                city = "NA",
                country = "EG",
                last_name = lastName,
                state = "NA"
            };

            var payment = new Payment
            {
                CourseId = course.Id,
                StudentId = user.Id,
                SubmittingDate = DateTime.UtcNow,
                TotalPrice = course.Price,
                PaymentMethod = normalizedMethod,
                PaymentStatus = "Pending",
                PaymentProvider = "Paymob",
                SpecialReference = specialReference,
                MerchantOrderId = merchantOrderId
            };

            try
            {
                using var authDocument = await SendPaymobRequest(
                    "authentication",
                    "auth/tokens",
                    new { api_key = paymob.ApiKey });
                var authToken = GetRequiredString(
                    authDocument.RootElement,
                    "token",
                    "PayMob authentication response did not contain a token.");

                using var orderDocument = await SendPaymobRequest(
                    "order creation",
                    "ecommerce/orders",
                    new
                    {
                        auth_token = authToken,
                        delivery_needed = false,
                        amount_cents = amountCents,
                        currency = paymob.Currency,
                        merchant_order_id = merchantOrderId,
                        items = new[]
                        {
                            new
                            {
                                name = course.Name,
                                amount_cents = amountCents,
                                description = $"EduVerse course enrollment: {course.Name}",
                                quantity = 1
                            }
                        }
                    });
                var providerOrderId = GetRequiredLong(
                    orderDocument.RootElement,
                    "id",
                    "PayMob order response did not contain an order id.");

                using var paymentKeyDocument = await SendPaymobRequest(
                    "payment key creation",
                    "acceptance/payment_keys",
                    new
                    {
                        auth_token = authToken,
                        amount_cents = amountCents,
                        expiration = Math.Max(paymob.PaymentKeyExpirationSeconds, 60),
                        order_id = providerOrderId,
                        billing_data = billingData,
                        currency = paymob.Currency,
                        integration_id = integrationId
                    });
                var paymentToken = GetRequiredString(
                    paymentKeyDocument.RootElement,
                    "token",
                    "PayMob payment-key response did not contain a token.");

                var redirectUrl =
                    $"{paymobClient.BaseAddress}acceptance/iframes/{paymob.IFrameId}" +
                    $"?payment_token={Uri.EscapeDataString(paymentToken)}";

                payment.ProviderIntentionId = providerOrderId.ToString();
                payment.ProviderStatusCode = (int)HttpStatusCode.OK;
                payment.ProviderResponse = JsonSerializer.Serialize(new
                {
                    orderId = providerOrderId,
                    integrationId,
                    status = "Pending"
                });
                payment.RedirectUrl = redirectUrl;
                await SavePaymentAsync(payment);

                logger.LogInformation(
                    "Created PayMob payment order {PaymobOrderId} for course {CourseId}, student {StudentId}, method {Method}",
                    providerOrderId,
                    course.Id,
                    user.Id,
                    normalizedMethod);

                return redirectUrl;
            }
            catch (PaymobException exception)
            {
                payment.PaymentStatus = "Failed";
                payment.ProviderStatusCode = exception.ProviderStatusCode;
                payment.ProviderResponse = exception.ProviderResponse;
                await SaveFailedPaymentSafely(payment);
                throw;
            }
        }

        public async Task<ServiceResponse> UpdatePaymentFromCallback(JsonElement callbackData, string? hmac)
        {
            ValidateCallbackConfiguration();
            if (!VerifyPaymobHmac(callbackData, hmac))
            {
                logger.LogWarning("Rejected PayMob callback because its HMAC signature was missing or invalid.");
                throw new PaymobException(
                    "PayMob callback HMAC validation failed.",
                    "Invalid payment callback signature.",
                    (int)HttpStatusCode.Unauthorized);
            }

            var transaction = GetCallbackTransaction(callbackData);
            var merchantOrderId = FindJsonPropertyAsString(callbackData, "merchant_order_id");
            var specialReference = FindJsonPropertyAsString(callbackData, "special_reference");
            var intentionId = FindJsonPropertyAsString(callbackData, "intention_id")
                ?? FindJsonPropertyAsString(callbackData, "payment_intention_id");
            var providerOrderId = GetNestedPropertyAsString(transaction, "order", "id");

            var payment = await Payments.Query(true).FirstOrDefaultAsync(p =>
                (!string.IsNullOrEmpty(merchantOrderId) && p.MerchantOrderId == merchantOrderId) ||
                (!string.IsNullOrEmpty(specialReference) && p.SpecialReference == specialReference) ||
                (!string.IsNullOrEmpty(intentionId) && p.ProviderIntentionId == intentionId) ||
                (!string.IsNullOrEmpty(providerOrderId) && p.ProviderIntentionId == providerOrderId));

            if (payment == null)
            {
                return new ServiceResponse(false, "Payment callback does not match any saved payment.");
            }

            var callbackAmount = FindJsonPropertyAsInt(callbackData, "amount_cents");
            var expectedAmount = checked((long)Math.Round(
                payment.TotalPrice * 100,
                MidpointRounding.AwayFromZero));
            if (callbackAmount == null || callbackAmount.Value != expectedAmount)
            {
                throw new PaymobException(
                    $"PayMob callback amount mismatch. Expected {expectedAmount}, received {callbackAmount?.ToString() ?? "missing"}.",
                    "Payment callback amount did not match the order.",
                    (int)HttpStatusCode.BadRequest);
            }

            var callbackIntegrationId = FindJsonPropertyAsInt(callbackData, "integration_id");
            var expectedIntegrationId = GetIntegrationId(payment.PaymentMethod);
            if (callbackIntegrationId == null || callbackIntegrationId.Value != expectedIntegrationId)
            {
                throw new PaymobException(
                    $"PayMob callback integration mismatch. Expected {expectedIntegrationId}, received {callbackIntegrationId?.ToString() ?? "missing"}.",
                    "Payment callback method did not match the order.",
                    (int)HttpStatusCode.BadRequest);
            }

            var success = FindJsonPropertyAsBool(transaction, "success");
            var pending = FindJsonPropertyAsBool(transaction, "pending");
            var errorOccured = FindJsonPropertyAsBool(transaction, "error_occured");

            payment.PaymentStatus = success == true
                ? "Paid"
                : pending == true
                    ? "Pending"
                    : errorOccured == true
                        ? "Failed"
                        : "Failed";

            payment.ProviderResponse = SanitizePaymobResponse(callbackData.GetRawText());

            var providerStatusCode = FindJsonPropertyAsInt(callbackData, "status_code");
            if (providerStatusCode != null)
            {
                payment.ProviderStatusCode = providerStatusCode;
            }

            var providerIntentionId = FindJsonPropertyAsString(callbackData, "intention_id")
                ?? FindJsonPropertyAsString(callbackData, "payment_intention_id");
            if (!string.IsNullOrEmpty(providerIntentionId))
            {
                payment.ProviderIntentionId = providerIntentionId;
            }

            await Payments.UpdateAsync(payment);
            if (payment.PaymentStatus == "Paid")
            {
                var existingEnrollment = await Enrollment.Query().FirstOrDefaultAsync(e => e.CourseId == payment.CourseId && e.StudentId == payment.StudentId);
                if (existingEnrollment == null)
                {
                    await Enrollment.AddAsync(new Enrollment
                    {
                        CourseId = payment.CourseId,
                        StudentId = payment.StudentId,
                        EnrollmentDate = DateTime.UtcNow,
                        Progression = 0,
                        ProgressPercentage = 0
                    });
                    await CreateNotification(payment.StudentId, "Course enrolled", "Payment confirmed and enrollment created.");
                }
            }

            logger.LogInformation(
                "Processed PayMob callback for course {CourseId}, student {StudentId}; status is {PaymentStatus}",
                payment.CourseId,
                payment.StudentId,
                payment.PaymentStatus);
            return new ServiceResponse(true, $"Payment status updated to {payment.PaymentStatus}.");
        }

        public async Task<IEnumerable<NotificationDto>> GetMyNotifications(string userId)
        {
            return await Notifications.Query()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ServiceResponse> MarkNotificationAsRead(Guid id, string userId)
        {
            var notification = await Notifications.GetByIdAsync(id);
            if (notification == null || notification.UserId != userId)
                return new ServiceResponse(false, "Notification not found.");

            notification.IsRead = true;
            await Notifications.UpdateAsync(notification);
            return new ServiceResponse(true, "Notification marked as read.");
        }

        public async Task<ServiceResponse> MarkAttendance(Guid sessionId, string userId, string attendanceCode)
        {
            if (sessionId == Guid.Empty || string.IsNullOrWhiteSpace(userId))
                return new ServiceResponse(false, "Invalid attendance request.");

            var session = await Sessions.Query().FirstOrDefaultAsync(item => item.Id == sessionId);
            if (session == null)
                return new ServiceResponse(false, "Session not found.");

            var isEnrolled = await Enrollment.Query()
                .AnyAsync(item => item.CourseId == session.CourseId && item.StudentId == userId);
            if (!isEnrolled)
                return new ServiceResponse(false, "You must be enrolled in this course to mark attendance.");

            if (string.IsNullOrWhiteSpace(session.AttendanceCode) ||
                !string.Equals(session.AttendanceCode, attendanceCode?.Trim(), StringComparison.Ordinal))
            {
                return new ServiceResponse(false, "Attendance code is invalid.");
            }

            var attendance = await AttendanceRecords.Query(true)
                .FirstOrDefaultAsync(item => item.SessionId == sessionId && item.StudentId == userId);
            if (attendance == null)
            {
                await AttendanceRecords.AddAsync(new AttendanceRecord
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    StudentId = userId,
                    Attended = true
                });
            }
            else if (!attendance.Attended)
            {
                attendance.Attended = true;
                await AttendanceRecords.UpdateAsync(attendance);
            }

            return new ServiceResponse(true, "Attendance marked successfully.");

        }
        public async Task <IEnumerable<AttendanceRecord>> GetAttendanceRecords(Guid sessionId)
        {
            var result = await AttendanceRecords.GetAllAsync();
            return result;
        }


        private async Task CreateNotification(string userId, string title, string message)
        {
            await Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                CreatedAt = DateTime.UtcNow
            });
        }

        private async Task<(int DoneSessions, int TotalSessions, double ProgressPercentage)> SyncEnrollmentProgress(
            Guid courseId,
            string userId,
            Enrollment enrollment,
            HashSet<Guid>? courseSessionIds = null)
        {
            var sessionIds = courseSessionIds ?? (await Sessions.Query()
                .Where(s => s.CourseId == courseId)
                .Select(s => s.Id)
                .ToListAsync()).ToHashSet();

            var totalSessions = sessionIds.Count;
            var progressRows = await Progresses.Query()
                .Where(p => p.CourseId == courseId && p.StudentId == userId)
                .ToListAsync();
            var doneSessions = progressRows.Count(p => p.IsDone && sessionIds.Contains(p.SessionId));
            var percentage = CalculatePercentage(doneSessions, totalSessions);

            enrollment.Progression = percentage;
            enrollment.ProgressPercentage = percentage;
            if (totalSessions > 0 && doneSessions == totalSessions)
            {
                MarkEnrollmentCompleted(enrollment);
            }
            else if (!enrollment.CompletedAt.HasValue && !enrollment.GraduationDate.HasValue)
            {
                enrollment.IsCompleted = false;
            }

            await Enrollment.UpdateAsync(enrollment);
            return (doneSessions, totalSessions, percentage);
        }

        private static bool IsEnrollmentCompleted(Enrollment enrollment)
        {
            return enrollment.IsCompleted || enrollment.CompletedAt.HasValue || enrollment.GraduationDate.HasValue || enrollment.Progression >= 100 || enrollment.ProgressPercentage >= 100;
        }

        private static bool IsCourseDurationFinished(Course course, Enrollment enrollment)
        {
            if (enrollment.IsCompleted || enrollment.CompletedAt.HasValue || enrollment.GraduationDate.HasValue)
                return true;

            if (course.Duration <= 0)
                return true;

            var enrollmentDate = enrollment.EnrollmentDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(enrollment.EnrollmentDate, DateTimeKind.Utc)
                : enrollment.EnrollmentDate.ToUniversalTime();

            return enrollmentDate.AddDays(course.Duration) <= DateTime.UtcNow;
        }

        private static double CalculatePercentage(int completed, int total)
        {
            return total == 0 ? 0 : Math.Round(completed * 100.0 / total, 2);
        }

        private static void MarkEnrollmentCompleted(Enrollment enrollment)
        {
            enrollment.Progression = 100;
            enrollment.ProgressPercentage = 100;
            enrollment.IsCompleted = true;
            enrollment.CompletedAt ??= DateTime.UtcNow;
            enrollment.GraduationDate ??= enrollment.CompletedAt;
        }

        private static string BuildCertificateDownloadPath(Guid certificateId)
        {
            return $"/Certificate/Download/{certificateId}";
        }

        private static string BuildCertificateDownloadUrl(string baseUrl, Guid certificateId)
        {
            return $"{baseUrl.TrimEnd('/')}{BuildCertificateDownloadPath(certificateId)}";
        }

        private static string SafeFileName(string value)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
            var safeValue = new string(value
                .Select(character => invalidCharacters.Contains(character) ? '-' : character)
                .ToArray());
            safeValue = string.Join("-", safeValue.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(safeValue) ? "Certificate" : safeValue[..Math.Min(safeValue.Length, 80)];
        }

        private static SessionMaterialDto ToMaterialDto(SessionMaterial material)
        {
            return new SessionMaterialDto
            {
                Id = material.Id,
                SessionId = material.SessionId,
                Title = material.Title,
                Type = material.Type,
                Url = material.Url,
                FilePath = material.FilePath,
                CreatedAt = material.CreatedAt
            };
        }

        private static StudentAssignmentDto BuildStudentAssignmentDto(Assignment assignment, Course course, Session session, AssignmentSubmission? submission)
        {
            var status = "Not Submitted";
            if (submission != null)
            {
                status = submission.Grade.HasValue ? "Graded" : submission.IsLate ? "Late" : "Submitted";
            }
            else if (assignment.DueDate.HasValue && DateTime.UtcNow > assignment.DueDate.Value)
            {
                status = "Missing";
            }

            return new StudentAssignmentDto
            {
                AssignmentId = assignment.Id,
                Title = assignment.Subject,
                Description = assignment.Description,
                CourseId = course.Id,
                CourseName = course.Name,
                SessionId = session.Id,
                SessionTitle = session.Title,
                SessionNumber = session.SessionNumber,
                DueDate = assignment.DueDate,
                SubmissionStatus = status,
                SubmittedAt = submission?.SubmittedAt,
                Grade = submission?.Grade,
                Feedback = submission?.Feedback,
                AssignmentFileUrl = string.IsNullOrWhiteSpace(assignment.Content) ? null : assignment.Content,
                FileUrl = submission?.FileUrl
            };
        }

        private async Task SavePaymentAsync(Payment payment)
        {
            var existingPayment = await Payments.Query()
                .FirstOrDefaultAsync(p => p.CourseId == payment.CourseId && p.StudentId == payment.StudentId);

            if (existingPayment == null)
            {
                await Payments.AddAsync(payment);
                return;
            }

            await Payments.UpdateAsync(payment);
        }

        private async Task SaveFailedPaymentSafely(Payment payment)
        {
            try
            {
                await SavePaymentAsync(payment);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to persist failed PayMob attempt for course {CourseId}, student {StudentId}",
                    payment.CourseId,
                    payment.StudentId);
            }
        }

        private async Task<JsonDocument> SendPaymobRequest(
            string stage,
            string endpoint,
            object payload)
        {
            HttpResponseMessage response;
            try
            {
                response = await paymobClient.PostAsJsonAsync(endpoint, payload);
            }
            catch (TaskCanceledException exception)
            {
                throw new PaymobException(
                    $"PayMob {stage} timed out.",
                    "The payment provider timed out. Please try again.",
                    (int)HttpStatusCode.GatewayTimeout,
                    innerException: exception);
            }
            catch (HttpRequestException exception)
            {
                throw new PaymobException(
                    $"Could not connect to PayMob during {stage}: {exception.Message}",
                    "Could not connect to the payment provider. Please try again.",
                    (int)HttpStatusCode.BadGateway,
                    innerException: exception);
            }

            using (response)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var sanitizedBody = SanitizePaymobResponse(responseBody);

                logger.LogInformation(
                    "PayMob {Stage} returned HTTP {StatusCode}",
                    stage,
                    (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "PayMob {Stage} failed with HTTP {StatusCode}. Response: {ProviderResponse}",
                        stage,
                        (int)response.StatusCode,
                        sanitizedBody);

                    var providerMessage = ExtractPaymobErrorMessage(responseBody);
                    throw new PaymobException(
                        $"PayMob {stage} failed with HTTP {(int)response.StatusCode}: {providerMessage}",
                        $"The payment provider rejected the {stage} request.",
                        (int)HttpStatusCode.BadGateway,
                        (int)response.StatusCode,
                        sanitizedBody);
                }

                logger.LogDebug(
                    "PayMob {Stage} response: {ProviderResponse}",
                    stage,
                    sanitizedBody);

                try
                {
                    return JsonDocument.Parse(responseBody);
                }
                catch (JsonException exception)
                {
                    throw new PaymobException(
                        $"PayMob {stage} returned invalid JSON.",
                        "The payment provider returned an invalid response.",
                        (int)HttpStatusCode.BadGateway,
                        (int)response.StatusCode,
                        sanitizedBody,
                        exception);
                }
            }
        }

        private void ValidatePaymobConfiguration()
        {
            var missingKeys = new List<string>();
            if (string.IsNullOrWhiteSpace(paymob.ApiKey))
                missingKeys.Add("Paymob:ApiKey");
            if (paymob.IFrameId <= 0)
                missingKeys.Add("Paymob:IFrameId");
            if (string.IsNullOrWhiteSpace(paymob.Currency))
                missingKeys.Add("Paymob:Currency");

            if (missingKeys.Count > 0)
            {
                throw new PaymobException(
                    $"PayMob configuration is missing or invalid: {string.Join(", ", missingKeys)}.",
                    "The payment provider is not configured.",
                    (int)HttpStatusCode.ServiceUnavailable);
            }
        }

        private void ValidateCallbackConfiguration()
        {
            if (string.IsNullOrWhiteSpace(paymob.HmacSecret))
            {
                throw new PaymobException(
                    "PayMob configuration is missing Paymob:HmacSecret.",
                    "Payment callback validation is not configured.",
                    (int)HttpStatusCode.ServiceUnavailable);
            }
        }

        private int GetIntegrationId(string method)
        {
            method = NormalizePaymentMethod(method);
            var integrationId = method switch
            {
                "card" => paymob.CardIntegrationId,
                "bank_transfer" => paymob.BankTransferIntegrationId,
                "installments" => paymob.InstallmentsIntegrationId,
                "wallet" => paymob.WalletIntegrationId,
                _ => throw new PaymobException(
                    $"Unsupported PayMob payment method '{method}'.",
                    "Unsupported payment method. Use card, bank_transfer, or installments.",
                    (int)HttpStatusCode.BadRequest)
            };

            if (integrationId <= 0)
            {
                throw new PaymobException(
                    $"PayMob integration id for method '{method}' is not configured.",
                    $"The payment method '{method}' is not configured.",
                    (int)HttpStatusCode.ServiceUnavailable);
            }

            return integrationId;
        }

        private static string NormalizePaymentMethod(string method)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                throw new PaymobException(
                    "Payment method is required.",
                    "Payment method is required.",
                    (int)HttpStatusCode.BadRequest);
            }

            var normalized = method.Trim()
                .ToLowerInvariant()
                .Replace("-", "_")
                .Replace(" ", "_");

            return normalized switch
            {
                "banktransfer" => "bank_transfer",
                "installment" => "installments",
                _ => normalized
            };
        }

        private static (string FirstName, string LastName) SplitFullName(string? fullName)
        {
            var parts = (fullName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length switch
            {
                0 => ("EduVerse", "Student"),
                1 => (parts[0], "Student"),
                _ => (parts[0], string.Join(' ', parts.Skip(1)))
            };
        }

        private static string GetRequiredString(
            JsonElement element,
            string propertyName,
            string errorMessage)
        {
            var value = GetJsonPropertyAsString(element, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            throw new PaymobException(
                errorMessage,
                "The payment provider returned an incomplete response.",
                (int)HttpStatusCode.BadGateway);
        }

        private static long GetRequiredLong(
            JsonElement element,
            string propertyName,
            string errorMessage)
        {
            var value = GetJsonPropertyAsString(element, propertyName);
            if (long.TryParse(value, out var result))
            {
                return result;
            }

            throw new PaymobException(
                errorMessage,
                "The payment provider returned an incomplete response.",
                (int)HttpStatusCode.BadGateway);
        }

        private bool VerifyPaymobHmac(JsonElement callbackData, string? providedHmac)
        {
            if (string.IsNullOrWhiteSpace(providedHmac))
            {
                return false;
            }

            var transaction = GetCallbackTransaction(callbackData);
            var canonicalValue = string.Concat(
                GetDirectPropertyAsString(transaction, "amount_cents"),
                GetDirectPropertyAsString(transaction, "created_at"),
                GetDirectPropertyAsString(transaction, "currency"),
                GetDirectPropertyAsString(transaction, "error_occured"),
                GetDirectPropertyAsString(transaction, "has_parent_transaction"),
                GetDirectPropertyAsString(transaction, "id"),
                GetDirectPropertyAsString(transaction, "integration_id"),
                GetDirectPropertyAsString(transaction, "is_3d_secure"),
                GetDirectPropertyAsString(transaction, "is_auth"),
                GetDirectPropertyAsString(transaction, "is_capture"),
                GetDirectPropertyAsString(transaction, "is_refunded"),
                GetDirectPropertyAsString(transaction, "is_standalone_payment"),
                GetDirectPropertyAsString(transaction, "is_voided"),
                GetNestedPropertyAsString(transaction, "order", "id"),
                GetDirectPropertyAsString(transaction, "owner"),
                GetDirectPropertyAsString(transaction, "pending"),
                GetNestedPropertyAsString(transaction, "source_data", "pan"),
                GetNestedPropertyAsString(transaction, "source_data", "sub_type"),
                GetNestedPropertyAsString(transaction, "source_data", "type"),
                GetDirectPropertyAsString(transaction, "success"));

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(paymob.HmacSecret));
            var expectedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonicalValue));

            try
            {
                var providedBytes = Convert.FromHexString(providedHmac.Trim());
                return providedBytes.Length == expectedBytes.Length
                    && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static JsonElement GetCallbackTransaction(JsonElement callbackData)
        {
            if (callbackData.ValueKind == JsonValueKind.Object
                && callbackData.TryGetProperty("obj", out var transaction)
                && transaction.ValueKind == JsonValueKind.Object)
            {
                return transaction;
            }

            return callbackData;
        }

        private static string GetDirectPropertyAsString(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return JsonElementToString(property.Value) ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private static string GetNestedPropertyAsString(
            JsonElement element,
            string objectProperty,
            string valueProperty)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(objectProperty, StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.Object)
                {
                    return GetDirectPropertyAsString(property.Value, valueProperty);
                }
            }

            return string.Empty;
        }

        private static string ExtractPaymobErrorMessage(string responseBody)
        {
            try
            {
                using var document = JsonDocument.Parse(responseBody);
                foreach (var propertyName in new[] { "message", "detail", "error", "errors" })
                {
                    var value = FindJsonPropertyAsString(document.RootElement, propertyName);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return Truncate(value, 500);
                    }
                }
            }
            catch (JsonException)
            {
                // Fall back to the raw, truncated provider response.
            }

            return string.IsNullOrWhiteSpace(responseBody)
                ? "No response body was returned."
                : Truncate(responseBody, 500);
        }

        private static string SanitizePaymobResponse(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return string.Empty;
            }

            try
            {
                var node = JsonNode.Parse(responseBody);
                RedactSensitivePaymobFields(node);
                return Truncate(node?.ToJsonString() ?? string.Empty, 4000);
            }
            catch (JsonException)
            {
                return Truncate(responseBody, 1000);
            }
        }

        private static void RedactSensitivePaymobFields(JsonNode? node)
        {
            if (node is JsonObject jsonObject)
            {
                foreach (var property in jsonObject.ToList())
                {
                    if (PaymobSensitiveFields.Contains(property.Key))
                    {
                        jsonObject[property.Key] = "<redacted>";
                    }
                    else
                    {
                        RedactSensitivePaymobFields(property.Value);
                    }
                }

                return;
            }

            if (node is JsonArray jsonArray)
            {
                foreach (var item in jsonArray)
                {
                    RedactSensitivePaymobFields(item);
                }
            }
        }

        private static string Truncate(string value, int maximumLength)
        {
            return value.Length <= maximumLength
                ? value
                : value[..maximumLength] + "...";
        }

        private static string? GetJsonPropertyAsString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => property.GetRawText()
            };
        }

        private static string? FindJsonPropertyAsString(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals(propertyName))
                    {
                        return JsonElementToString(property.Value);
                    }

                    var nestedValue = FindJsonPropertyAsString(property.Value, propertyName);
                    if (!string.IsNullOrEmpty(nestedValue))
                    {
                        return nestedValue;
                    }
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nestedValue = FindJsonPropertyAsString(item, propertyName);
                    if (!string.IsNullOrEmpty(nestedValue))
                    {
                        return nestedValue;
                    }
                }
            }

            return null;
        }

        private static string? JsonElementToString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                _ => element.GetRawText()
            };
        }

        private static bool? FindJsonPropertyAsBool(JsonElement element, string propertyName)
        {
            var value = FindJsonPropertyAsString(element, propertyName);
            return bool.TryParse(value, out var parsedValue) ? parsedValue : null;
        }

        private static int? FindJsonPropertyAsInt(JsonElement element, string propertyName)
        {
            var value = FindJsonPropertyAsString(element, propertyName);
            return int.TryParse(value, out var parsedValue) ? parsedValue : null;
        }

    }
}
 
