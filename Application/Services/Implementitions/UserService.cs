using Application.DTOs.Auth;

using Application.DTOs.Cloud;
using Application.DTOs.Course;
using Application.DTOs.Enrollments;
using Application.DTOs.Learning;
using Application.DTOs.Payment;
using Application.DTOs.Responses;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Services.Implementitions
{
    public class UserService(IGeneric<Enrollment> Enrollment, ICloudService cloud
        , IGeneric<Course> Courses, IUserManagment userManagment, IGeneric<AssignmentSubmission> AssignmentSubmission,
        IGeneric<Payment> Payments, IMapper mapper, IHttpClientFactory httpClientFactory,
        IGeneric<Session> Sessions, IGeneric<Assignment> Assignments,
        IGeneric<StudentSessionProgress> Progresses, IGeneric<CertificateRecord> Certificates,
        IGeneric<Notification> Notifications, IGeneric<AttendanceRecord> AttendanceRecords,
        IGeneric<SessionMaterial> SessionMaterials) : IUserService
    {
        private readonly string PaymobApi = "ZXlKaGJHY2lPaUpJVXpVeE1pSXNJblI1Y0NJNklrcFhWQ0o5LmV5SmpiR0Z6Y3lJNklrMWxjbU5vWVc1MElpd2ljSEp2Wm1sc1pWOXdheUk2TVRFME16UTBNeXdpYm1GdFpTSTZJbWx1YVhScFlXd2lmUS5rSm9SRWNtUG8xVHhjR3lKMFg2NXViM0VXYnZ3SEJMVnRSQ1FCMEthZHlCajRJRHRLMWZyU3A3NFE2Z3o2MjhENnVZOWszUnhKYWVfSnNKalhvTUV3QQ==";
        private readonly string PaymobSecret = "egy_sk_test_9a566c37c5a5706e567093e1bb650191de352802284e30fd7f6b0bd1c18d7a7e";
        private readonly string PaymobPublic = "egy_pk_test_3toKrv5jW8B0FcHVRTZYI12gK33a5Yvn";
        private readonly HttpClient httpClient = new HttpClient();
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
            var enrollment = (await Enrollment.GetAllAsync()).FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null || string.IsNullOrEmpty(enrollment.FileUrl))
            {
                return null;
            }
            var fileUrl = enrollment.FileUrl;
            return fileUrl;
        }

        public async Task<IEnumerable<GetCourse>> GetEnrolledCourses(string userId)
        {
            var enrollments = (await Enrollment.GetAllAsync()).Where(e => e.StudentId == userId).ToList();
            var courseIds = enrollments.Select(e => e.CourseId).ToList();
            var courses = (await Courses.GetAllAsync()).Where(c => !c.IsDeleted && courseIds.Contains(c.Id)).ToList();
            var enrolledCourses = mapper.Map<IEnumerable<GetCourse>>(courses);
            return enrolledCourses;
        }

        public async Task<IEnumerable<GetUser>> GetEnrolledUsers(Guid courseId)
        {
            var enrollments = (await Enrollment.GetAllAsync()).Where(e => e.CourseId == courseId).ToList();
            var userIds = enrollments.Select(e => e.StudentId).ToList();
            var users = (await userManagment.GetAllUsers()).Where(u => userIds.Contains(u.Id)).ToList();
            var enrolledUsers = mapper.Map<IEnumerable<GetUser>>(users);
            return enrolledUsers;
        }

        public async Task<Enrollment> GetEnrollmentData(Guid courseId, string Email)
        {
            var user = await userManagment.GetUserByEmail(Email);
            var userId = user.Id.ToString();
            var enrollment = (await Enrollment.GetAllAsync()).FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);
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

            var enrollments = (await Enrollment.GetAllAsync()).Where(e => e.StudentId == userId && !string.IsNullOrEmpty(e.FileUrl)).ToList();
            var certificateUrls = enrollments.Select(e => e.FileUrl).ToList();
            return certificateUrls;
        }

        public async Task<IEnumerable<CertificateDto>> GetMyCertificates(string userId, string baseUrl)
        {
            var certificates = (await Certificates.GetAllAsync()).Where(c => c.StudentId == userId).OrderByDescending(c => c.IssuedAt).ToList();
            var courses = (await Courses.GetAllAsync()).ToDictionary(c => c.Id, c => c);
            var user = await userManagment.GetUserById(userId);
            return certificates.Select(c => new CertificateDto
            {
                Id = c.Id,
                CourseId = c.CourseId,
                CourseName = courses.TryGetValue(c.CourseId, out var course) ? course.Name : "Course",
                StudentName = user?.FullName ?? user?.Email ?? "Student",
                CertificateCode = c.CertificateCode,
                IssuedAt = c.IssuedAt,
                FileUrl = c.FileUrl,
                Status = c.Status,
                VerificationUrl = $"{baseUrl.TrimEnd('/')}/Certificate/Verify/{Uri.EscapeDataString(c.CertificateCode)}"
            });
        }

        public async Task<ServiceResponse> GenerateCertificate(Guid courseId, string userId, string baseUrl)
        {
            var enrollment = (await Enrollment.GetAllAsync()).FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null)
                return new ServiceResponse(false, "Enrollment not found.");

            if (!IsEnrollmentCompleted(enrollment))
                return new ServiceResponse(false, "Course must be completed before generating a certificate.");

            var existing = (await Certificates.GetAllAsync()).FirstOrDefault(c => c.CourseId == courseId && c.StudentId == userId);
            if (existing != null)
                return new ServiceResponse(true, "Certificate already exists.", (await GetMyCertificates(userId, baseUrl)).FirstOrDefault(c => c.CourseId == courseId));

            var code = $"EDU-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
            var certificate = new CertificateRecord
            {
                StudentId = userId,
                CourseId = courseId,
                CertificateCode = code,
                IssuedAt = DateTime.UtcNow,
                Status = "Valid"
            };
            await Certificates.AddAsync(certificate);
            enrollment.CertificateCode = code;
            enrollment.GraduationDate ??= DateTime.UtcNow;
            await Enrollment.UpdateAsync(enrollment);
            await CreateNotification(userId, "Certificate issued", "Your course certificate is ready.");
            return new ServiceResponse(true, "Certificate generated successfully.", (await GetMyCertificates(userId, baseUrl)).FirstOrDefault(c => c.CourseId == courseId));
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

            var sessions = (await Sessions.GetAllAsync()).Where(s => s.CourseId == courseId).OrderBy(s => s.SessionNumber).ToList();
            var progressRows = (await Progresses.GetAllAsync()).Where(p => p.CourseId == courseId && p.StudentId == userId).ToList();
            var sessionIds = sessions.Select(s => s.Id).ToHashSet();
            var assignments = (await Assignments.GetAllAsync()).Where(a => sessionIds.Contains(a.SessionId)).ToList();
            var submissions = (await AssignmentSubmission.GetAllAsync()).Where(s => s.StudentId == userId).ToList();
            var materials = (await SessionMaterials.GetAllAsync()).Where(m => sessionIds.Contains(m.SessionId)).ToList();

            return new CourseProgressDto
            {
                CourseId = courseId,
                CourseName = course.Name,
                ProgressPercentage = enrollment.ProgressPercentage > 0 ? enrollment.ProgressPercentage : enrollment.Progression,
                IsCompleted = IsEnrollmentCompleted(enrollment),
                CompletedAt = enrollment.CompletedAt ?? enrollment.GraduationDate,
                Sessions = sessions.Select(session =>
                {
                    var progress = progressRows.FirstOrDefault(p => p.SessionId == session.Id);
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
                        IsCompleted = progress?.IsCompleted ?? false,
                        CompletedAt = progress?.CompletedAt,
                        Materials = materials.Where(m => m.SessionId == session.Id).Select(ToMaterialDto).ToList(),
                        Assignments = assignments.Where(a => a.SessionId == session.Id).Select(a => BuildStudentAssignmentDto(a, course, session, submissions.FirstOrDefault(s => s.AssignmentId == a.Id))).ToList()
                    };
                }).ToList()
            };
        }

        public async Task<ServiceResponse> MarkSessionCompleted(Guid sessionId, string userId)
        {
            var session = await Sessions.GetByIdAsync(sessionId);
            if (session == null)
                return new ServiceResponse(false, "Session not found.");

            var enrollment = (await Enrollment.GetAllAsync()).FirstOrDefault(e => e.CourseId == session.CourseId && e.StudentId == userId);
            if (enrollment == null)
                return new ServiceResponse(false, "You must enroll in this course before marking sessions completed.");

            var existing = (await Progresses.GetAllAsync()).FirstOrDefault(p => p.StudentId == userId && p.SessionId == sessionId);
            if (existing == null)
            {
                await Progresses.AddAsync(new StudentSessionProgress
                {
                    StudentId = userId,
                    CourseId = session.CourseId,
                    SessionId = sessionId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                });
            }
            else if (!existing.IsCompleted)
            {
                existing.IsCompleted = true;
                existing.CompletedAt = DateTime.UtcNow;
                await Progresses.UpdateAsync(existing);
            }

            var totalSessions = (await Sessions.GetAllAsync()).Count(s => s.CourseId == session.CourseId);
            var completedSessions = (await Progresses.GetAllAsync()).Count(p => p.CourseId == session.CourseId && p.StudentId == userId && p.IsCompleted);
            enrollment.ProgressPercentage = totalSessions == 0 ? 0 : Math.Round(completedSessions * 100.0 / totalSessions, 2);
            enrollment.Progression = enrollment.ProgressPercentage;
            if (enrollment.ProgressPercentage >= 100)
                MarkEnrollmentCompleted(enrollment);
            await Enrollment.UpdateAsync(enrollment);

            return new ServiceResponse(true, "Session marked as completed.", await GetCourseProgress(session.CourseId, userId));
        }

        public async Task<IEnumerable<StudentAssignmentDto>> GetMyAssignments(string userId)
        {
            var enrollments = (await Enrollment.GetAllAsync()).Where(e => e.StudentId == userId).ToList();
            var courseIds = enrollments.Select(e => e.CourseId).ToHashSet();
            var courses = (await Courses.GetAllAsync()).Where(c => !c.IsDeleted && courseIds.Contains(c.Id)).ToDictionary(c => c.Id, c => c);
            var sessions = (await Sessions.GetAllAsync()).Where(s => courseIds.Contains(s.CourseId)).ToList();
            var sessionById = sessions.ToDictionary(s => s.Id, s => s);
            var assignments = (await Assignments.GetAllAsync()).Where(a => sessionById.ContainsKey(a.SessionId)).ToList();
            var submissions = (await AssignmentSubmission.GetAllAsync()).Where(s => s.StudentId == userId).ToList();

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

            var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
            if (assignment == null)
                return new ServiceResponse(false, "Assignment not found.");

            var session = await Sessions.GetByIdAsync(assignment.SessionId);
            if (session == null)
                return new ServiceResponse(false, "Session not found.");

            var enrolled = (await Enrollment.GetAllAsync()).Any(e => e.CourseId == session.CourseId && e.StudentId == userId);
            if (!enrolled)
                return new ServiceResponse(false, "You can submit assignments only for enrolled courses.");

            var create = new CreateAssignmentSubmission
            {
                AssignmentId = submission.AssignmentId,
                StudentId = userId,
                File = submission.File,
                TextAnswer = submission.TextAnswer
            };

            return await SubmitAssignment(create);
        }
        public async Task<IEnumerable<GetAssignmentSubmission>> GetAssignmentSubmissions(Guid Id)
        {
            if (Id == Guid.Empty)
            {
                return null;
            }
            var submissions = (await AssignmentSubmission.GetAllAsync()).Where(s => s.AssignmentId == Id).ToList();
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

            var payments = (await Payments.GetAllAsync())
                .Where(p => p.StudentId == userId)
                .OrderByDescending(p => p.SubmittingDate)
                .ToList();

            return mapper.Map<IEnumerable<GetPayment>>(payments);
        }

        public async Task<IEnumerable<GetPayment>> GetCoursePayments(Guid courseId)
        {
            if (courseId == Guid.Empty)
            {
                return [];
            }

            var payments = (await Payments.GetAllAsync())
                .Where(p => p.CourseId == courseId)
                .OrderByDescending(p => p.SubmittingDate)
                .ToList();

            return mapper.Map<IEnumerable<GetPayment>>(payments);
        }

        public async Task<GetPayment> GetPayment(Guid courseId, string userId)
        {
            if (courseId == Guid.Empty || string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var payment = (await Payments.GetAllAsync())
                .FirstOrDefault(p => p.CourseId == courseId && p.StudentId == userId);

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
            var submissions = (await AssignmentSubmission.GetAllAsync()).Where(s => s.StudentId == userId).ToList();
            if (submissions == null || submissions.Count == 0)
            {
                return null;
            }
            var mappedSubmissions = mapper.Map<IEnumerable<GetAssignmentSubmission>>(submissions);
            return mappedSubmissions;
        }

        public async Task<ServiceResponse> SubmitAssignment(CreateAssignmentSubmission submission)
        {
            if (submission == null)
            {
                return new ServiceResponse(false, "Invalid submission data.");

            }
            var existingSubmission = (await AssignmentSubmission.GetAllAsync()).FirstOrDefault(s => s.AssignmentId == submission.AssignmentId && s.StudentId == submission.StudentId);
            if (existingSubmission != null)
            {
                var mapped = mapper.Map<AssignmentSubmission>(submission);
                if (submission.File != null)
                {
                    var fileDetails = new FileDetails
                    {
                        FileName = $"{submission.StudentId}_{submission.AssignmentId}_Submission.pdf",
                        Folder = "submissions"
                    };
                    var file = new AddCloudFile
                    {
                        File = submission.File,
                        Details = fileDetails
                    };
                    var deletefile = new FileDetails
                    {
                        FileName = existingSubmission.FileUrl,
                        Folder = "submissions"
                    };
                    var deleteResult = await cloud.DeleteFileAsync(deletefile);
                    var uploadResult = await cloud.UploadFileAsync(file);
                    if (!uploadResult.success)
                    {
                        return new ServiceResponse(false, "File upload failed.");
                    }
                    mapped.FileUrl = fileDetails.FileName;
                    existingSubmission.FileUrl = fileDetails.FileName;
                }
                existingSubmission.TextAnswer = submission.TextAnswer ?? existingSubmission.TextAnswer;
                existingSubmission.SubmittedAt = DateTime.UtcNow;
                var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
                existingSubmission.IsLate = assignment?.DueDate.HasValue == true && DateTime.UtcNow > assignment.DueDate.Value;
                var updateResult = await AssignmentSubmission.UpdateAsync(existingSubmission);
                if (updateResult != null)
                {
                    return new ServiceResponse(true, "Assignment submission updated successfully.");
                }
                else
                {
                    return new ServiceResponse(false, "Failed to update assignment submission.");
                }


            }
            else
            {
                var mapped = mapper.Map<AssignmentSubmission>(submission);
                if (submission.File != null)
                {
                    var fileDetails = new FileDetails
                    {
                        FileName = $"{submission.StudentId}_{submission.AssignmentId}_Submission{Path.GetExtension(submission.File.FileName)}",
                        Folder = "submissions"
                    };
                    var file = new AddCloudFile
                    {
                        File = submission.File,
                        Details = fileDetails
                    };
                    var uploadResult = await cloud.UploadFileAsync(file);
                    if (!uploadResult.success)
                    {
                        return new ServiceResponse(false, "File upload failed.");
                    }
                    mapped.FileUrl = fileDetails.FileName;
                }
                else
                {
                    mapped.FileUrl = string.Empty;
                }
                mapped.TextAnswer = submission.TextAnswer;
                mapped.SubmittedAt = DateTime.UtcNow;
                var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
                mapped.IsLate = assignment?.DueDate.HasValue == true && DateTime.UtcNow > assignment.DueDate.Value;
                var result = await AssignmentSubmission.AddAsync(mapped);
                if (result != null)
                {
                    return new ServiceResponse(true, "Assignment submitted successfully.");
                }
                else
                {
                    return new ServiceResponse(false, "Failed to submit assignment.");
                }
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
            var submission = (await AssignmentSubmission.GetAllAsync()).FirstOrDefault(s => s.AssignmentId == Id && s.StudentId == userId);
            if (submission == null)
            {
                return null;
            }
            var mappedSubmission = mapper.Map<GetAssignmentSubmission>(submission);
            return mappedSubmission;
        }
        public async Task<string> Payment(string userId, Guid Course,string Method)
        {
            var course = await Courses.GetByIdAsync(Course);
            var User = await userManagment.GetUserById(userId);
            if (course == null || course.IsDeleted || User == null)
            {
                return null;
            }
            if (course.Price <= 0)
            {
                var enrollResult = await Enroll(Course, userId);
                return enrollResult.success ? "Free enrollment completed." : null;
            }
            var integrationId = DetermineIntegrationId(Method);
            if (integrationId == null)
            {
                return null;
            }
            int specialreference = new Random().Next(100000, 999999);
            var merchantOrderId = specialreference.ToString();
            

            // Prepare billing data
            var billingData = new
            {
                apartment = "N/A",
                first_name = User.FullName,
                last_name = "N/A",
                street = "N/A",
                building = "N/A",
                phone_number = User.PhoneNumber,
                country = "N/A",
                email = User.Email,
                floor = "N/A",
                state = "N/A",
                city = "N/A"
            };

            // Prepare intention request payload
            var payload = new
            {
                amount = course.Price,
                currency = "EGP",
                payment_methods = new[] { integrationId.Value },
                billing_data = billingData,
                items = new[]
                {
                    new
                    {
                        name = $"Enrollment #{course.Id}-{User.Id}",
                        amount = course.Price,
                        description = $"Course Enrollment Payment for course #{course.Name}",
                        quantity = 1
                    }
                },
                customer = new
                {
                    first_name = billingData.first_name,
                    last_name = billingData.last_name,
                    email = billingData.email,
                    
                },
                extras = new
                {
                    
                    customerId = User.Id
                },
                special_reference = specialreference,
                expiration = 3600, // 1 hour expiration
                merchant_order_id = merchantOrderId
            };
            var requestMessage = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://accept.paymob.com/v1/intention/");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Token", PaymobSecret);
            requestMessage.Content = JsonContent.Create(payload);
            var response = await httpClient.SendAsync(requestMessage);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var payment = new Payment
            {
                CourseId = course.Id,
                StudentId = User.Id,
                SubmittingDate = DateTime.Now,
                TotalPrice = course.Price,
                PaymentMethod = Method,
                PaymentStatus = response.IsSuccessStatusCode ? "Pending" : "Failed",
                PaymentProvider = "Paymob",
                SpecialReference = specialreference.ToString(),
                MerchantOrderId = merchantOrderId,
                ProviderStatusCode = (int)response.StatusCode,
                ProviderResponse = responseContent
            };

            if (!response.IsSuccessStatusCode)
            {
                await SavePaymentAsync(payment);
                return null;
            }

            // Parse the response to get client_secret
            var resultJson = JsonDocument.Parse(responseContent);
            var clientSecret = resultJson.RootElement.GetProperty("client_secret").GetString();
            string redirectUrl = $"https://accept.paymob.com/unifiedcheckout/?publicKey={PaymobPublic}&clientSecret={clientSecret}";
            payment.ProviderClientSecret = clientSecret;
            payment.ProviderIntentionId = GetJsonPropertyAsString(resultJson.RootElement, "id");
            payment.RedirectUrl = redirectUrl;
            await SavePaymentAsync(payment);
            return redirectUrl;

        }

        public async Task<ServiceResponse> UpdatePaymentFromCallback(JsonElement callbackData)
        {
            var merchantOrderId = FindJsonPropertyAsString(callbackData, "merchant_order_id");
            var specialReference = FindJsonPropertyAsString(callbackData, "special_reference");
            var intentionId = FindJsonPropertyAsString(callbackData, "intention_id")
                ?? FindJsonPropertyAsString(callbackData, "payment_intention_id")
                ?? FindJsonPropertyAsString(callbackData, "id");

            var payments = await Payments.GetAllAsync();
            var payment = payments.FirstOrDefault(p =>
                (!string.IsNullOrEmpty(merchantOrderId) && p.MerchantOrderId == merchantOrderId) ||
                (!string.IsNullOrEmpty(specialReference) && p.SpecialReference == specialReference) ||
                (!string.IsNullOrEmpty(intentionId) && p.ProviderIntentionId == intentionId));

            if (payment == null)
            {
                return new ServiceResponse(false, "Payment callback does not match any saved payment.");
            }

            var success = FindJsonPropertyAsBool(callbackData, "success");
            var pending = FindJsonPropertyAsBool(callbackData, "pending");
            var errorOccured = FindJsonPropertyAsBool(callbackData, "error_occured");

            payment.PaymentStatus = success == true
                ? "Paid"
                : pending == true
                    ? "Pending"
                    : errorOccured == true
                        ? "Failed"
                        : "Failed";

            payment.ProviderResponse = callbackData.GetRawText();

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
                var existingEnrollment = (await Enrollment.GetAllAsync()).FirstOrDefault(e => e.CourseId == payment.CourseId && e.StudentId == payment.StudentId);
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
            return new ServiceResponse(true, $"Payment status updated to {payment.PaymentStatus}.");
        }

        public async Task<IEnumerable<NotificationDto>> GetMyNotifications(string userId)
        {
            return (await Notifications.GetAllAsync())
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
                .ToList();
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

        public async Task<ServiceResponse> MarkAttendance(Guid sessionId, string userId)
        {
           var attendance = (await AttendanceRecords.GetAllAsync()).FirstOrDefault(a => a.SessionId == sessionId && a.StudentId == userId);
            if ( attendance.Attended)
            {
                attendance.Attended = false;
                var result = await AttendanceRecords.UpdateAsync(attendance);

            }
            else
            {
                attendance.Attended = true;
                var result = await AttendanceRecords.UpdateAsync(attendance);
            }
            return new ServiceResponse(true, "Attendance status updated.");


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

        private static bool IsEnrollmentCompleted(Enrollment enrollment)
        {
            return enrollment.IsCompleted || enrollment.CompletedAt.HasValue || enrollment.GraduationDate.HasValue || enrollment.Progression >= 100 || enrollment.ProgressPercentage >= 100;
        }

        private static void MarkEnrollmentCompleted(Enrollment enrollment)
        {
            enrollment.Progression = 100;
            enrollment.ProgressPercentage = 100;
            enrollment.IsCompleted = true;
            enrollment.CompletedAt ??= DateTime.UtcNow;
            enrollment.GraduationDate ??= enrollment.CompletedAt;
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
                FileUrl = submission?.FileUrl
            };
        }

        private async Task SavePaymentAsync(Payment payment)
        {
            var existingPayment = (await Payments.GetAllAsync())
                .FirstOrDefault(p => p.CourseId == payment.CourseId && p.StudentId == payment.StudentId);

            if (existingPayment == null)
            {
                await Payments.AddAsync(payment);
                return;
            }

            await Payments.UpdateAsync(payment);
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

        private async Task<string> GetPaymobToken()
        {
            throw new NotImplementedException();

        }
        private int? DetermineIntegrationId(string Method)
        {
            if (string.IsNullOrWhiteSpace(Method))
            {
                return null;
            }

            // This is a placeholder implementation. You should replace this with your actual logic to determine the integration ID based on the course name.
            return Method.ToLowerInvariant() switch
            {
                "wallet" => 5597636,
                "card" => 5587071,
                _ => null
            };
        }
    }
}
 
