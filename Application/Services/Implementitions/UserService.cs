using Application.DTOs.Auth;

using Application.DTOs.Cloud;
using Application.DTOs.Course;
using Application.DTOs.Enrollments;
using Application.DTOs.Responses;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implementitions
{
    public  class UserService(IGeneric<Enrollment> Enrollment,ICloudService cloud
        ,IGeneric<Course> Courses , IUserManagment userManagment,IGeneric<AssignmentSubmission> AssignmentSubmission,
        IMapper mapper) : IUserService
    {
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
            if (existingEnrollment==null)
            {
                return new ServiceResponse(false, "User is not enrolled in the course.");
            }
            var fileDetails=new FileDetails
            {
                FileName = $"{userId}_{certificate.CourseId}_Certificate.pdf",
                Folder = "certificates",

            };
            var file=new AddCloudFile
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
                 FileUrl= fileDetails.FileName,
                GraduationDate = DateTime.Now,
                Progression=existingEnrollment.Progression,
                EnrollmentDate= existingEnrollment.EnrollmentDate


            };
            var updateResult = await Enrollment.UpdateAsync(newenrollment);
            if (updateResult!=null)
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
            if(courseId == Guid.Empty || string.IsNullOrEmpty(userId))
            {
                return new ServiceResponse(false, "Invalid course ID or user ID.");
            }
            var enrollment = new Enrollment
            {
                CourseId = courseId,
                StudentId = userId,
                EnrollmentDate = DateTime.Now,
                Progression = 0
            };
            var result = await Enrollment.AddAsync(enrollment);
            if (result!=null)
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
            var courses = (await Courses.GetAllAsync()).Where(c => courseIds.Contains(c.Id)).ToList();
            var enrolledCourses =mapper.Map<IEnumerable<GetCourse>>(courses);
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
                    var deletefile=new FileDetails
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
            var submission = (await AssignmentSubmission.GetAllAsync()).FirstOrDefault(s => s.AssignmentId== Id && s.StudentId == userId);
            if (submission == null)
            {
                return null;
            }
            var mappedSubmission = mapper.Map<GetAssignmentSubmission>(submission);
            return mappedSubmission;
        }

    }
}
