using Application.DTOs.Auth;

using Application.DTOs.Cloud;
using Application.DTOs.Course;
using Application.DTOs.Enrollments;
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
        IGeneric<Payment> Payments, IMapper mapper, IHttpClientFactory httpClientFactory) : IUserService
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
            var enrollment = new Enrollment
            {
                CourseId = courseId,
                StudentId = userId,
                EnrollmentDate = DateTime.Now,
                Progression = 0
            };
            var result = await Enrollment.AddAsync(enrollment);
            if (result != null)
            {
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

            }
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
                var updateResult = await AssignmentSubmission.UpdateAsync(mapped);
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
                var uploadResult = await cloud.UploadFileAsync(file);
                if (!uploadResult.success)
                {
                    return new ServiceResponse(false, "File upload failed.");
                }
                mapped.FileUrl = fileDetails.FileName;
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
            return new ServiceResponse(true, $"Payment status updated to {payment.PaymentStatus}.");
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
 
