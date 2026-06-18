using Application.DTOs.Assignment;
using Application.DTOs.Category;
using Application.DTOs.Cloud;
using Application.DTOs.Course;
using Application.DTOs.Payment;
using Application.DTOs.Rating;
using Application.DTOs.Responses;
using Application.DTOs.Sessions;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediaToolkit;
using MediaToolkit.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implementitions
{
    public  class CourseService(IGeneric<Course> CoursesManagment ,
        IGeneric<CourseCategory> CoursesCatManagment,IGeneric<Category> CategoryManagment,
        IMapper mapper ,ICloudService cloud,IGeneric<Rating> RatingManagment ,IGeneric<Session> SessionManagment,IGeneric<Assignment> AssignmentManagment,
        IGeneric<Enrollment> EnrollmentManagment, IGeneric<Payment> PaymentManagment, IUserManagment UserManagment,
        IActivityLogService activityLogService,IGeneric<AttendanceRecord> attendencemanagment,
        IGeneric<Organization> OrganizationManagment, IGeneric<StudentSessionProgress> ProgressManagment,
        ILogger<CourseService> logger) : ICourseService
    {
        public async Task<ServiceResponse> AddRating(CreateRating rating, string userid)
        {
            if(rating == null || rating.CourseId == Guid.Empty || string.IsNullOrEmpty(userid))
                return new ServiceResponse { success = false, message = "Invalid rating data" };
            if (rating.RatingValue < 1 || rating.RatingValue > 5)
                return new ServiceResponse { success = false, message = "Rating must be between 1 and 5." };
            var course = await CoursesManagment.GetByIdAsync(rating.CourseId);
            if (course == null || course.IsDeleted)
                return new ServiceResponse { success = false, message = "Course not found" };
            var enrollment = (await EnrollmentManagment.GetAllAsync())
                .FirstOrDefault(e => e.CourseId == rating.CourseId && e.StudentId == userid);
            if (enrollment == null)
                return new ServiceResponse { success = false, message = "You must enroll in this course before rating it." };
            if (!IsEnrollmentCompleted(enrollment))
                return new ServiceResponse { success = false, message = "You can rate this course after completing it." };

            var existingRatings = (await RatingManagment.GetAllAsync()).ToList();
            var userCourseRating = existingRatings.FirstOrDefault(r => r.CourseId == rating.CourseId && r.StudentId == userid);
            if (userCourseRating != null)
            {
                userCourseRating.RatingValue = rating.RatingValue;
                await RatingManagment.UpdateAsync(userCourseRating);
            }
            else
            {
                var mappedRating = mapper.Map<Rating>(rating);
                mappedRating.StudentId = userid;
                await RatingManagment.AddAsync(mappedRating);
            }

            var updatedRatings = (await RatingManagment.GetAllAsync())
                .Where(r => r.CourseId == rating.CourseId)
                .ToList();
            var result = updatedRatings.FirstOrDefault(r => r.StudentId == userid);
            if (result == null)
                return new ServiceResponse { success = false, message = "Failed to save rating" };

            return new ServiceResponse
            {
                success = true,
                message = userCourseRating != null ? "Rating updated successfully" : "Rating added successfully",
                data = new
                {
                    courseId = rating.CourseId,
                    averageRating = updatedRatings.Any() ? Math.Round(updatedRatings.Average(r => r.RatingValue), 2) : 0,
                    ratingCount = updatedRatings.Count,
                    userRating = result.RatingValue
                }
            };
        }

        

        public  async Task<ServiceResponse> CreateCourse(CreateCourse Course, string currentUserId, bool isAdmin)
        {
            if (Course == null)
                return new ServiceResponse { success = false, message = "Course data is null" };
            if (string.IsNullOrEmpty(currentUserId))
                return new ServiceResponse { success = false, message = "Current user is required" };

            var creator = await UserManagment.GetUserById(currentUserId);
            if (creator == null)
                return new ServiceResponse { success = false, message = "Current user not found" };

            Guid? targetOrganizationId = null;
            if (isAdmin && Course.OrganizationId.HasValue)
            {
                var targetOrganization = await OrganizationManagment.GetByIdAsync(Course.OrganizationId.Value);
                if (targetOrganization == null)
                    return new ServiceResponse { success = false, message = "Organization not found" };
                targetOrganizationId = targetOrganization.Id;
            }
            else
            {
                if (!creator.OrganizationId.HasValue)
                    return new ServiceResponse { success = false, message = "User is not assigned to an organization" };
                targetOrganizationId = creator.OrganizationId.Value;
            }

            var mapping = mapper.Map<Course>(Course);
            mapping.Duration = 0;
            mapping.IsDeleted = false;
            mapping.ImageUrl = $"{mapping.Id}-Thumbnail";
            mapping.OrgId = currentUserId;
            mapping.OrganizationId = targetOrganizationId;
            var details =new FileDetails { FileName = mapping.ImageUrl, Folder = "courses" };
            var AddCloudFile = new AddCloudFile { Details = details, File = Course.Image};
            var uploadResult = await cloud.UploadFileAsync(AddCloudFile);
            if (!uploadResult.success)
                return new ServiceResponse { success = false, message = "Failed to upload course image" };
            var result = await CoursesManagment.AddAsync(mapping);
            if (result == null)
                return new ServiceResponse { success = false, message = "Failed to create Course" };
            
            var categoryIds = Course.Categories.Split(',').ToList();
            foreach (var category in categoryIds)
            {
                Guid categoryId;
                if (!Guid.TryParse(category, out categoryId))
                    continue;
                var test=await CategoryManagment.GetByIdAsync(categoryId);
                if(test == null)
                    continue;

                var courseCategory = new CourseCategory { CourseId =result.Id , CategoryId = test.Id };
                await CoursesCatManagment.AddAsync(courseCategory);
            }
            await activityLogService.LogAsync(currentUserId, DisplayName(creator), "CourseCreated", "Course", result.Id.ToString(), $"{result.Name} was created");
            return new ServiceResponse
            {
                success = true,
                message = $"Course created successfully. CourseId: {result.Id}",
                data = result.Id.ToString()
            };


        }

        public async Task<ServiceResponse> DeleteCourse(Guid id, string deletedById, string deletedByName)
        {
            if (id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid Course ID" };
            var course = await CoursesManagment.GetByIdAsync(id);
            if (course == null)
                return new ServiceResponse { success = false, message = "Course not found" };
            if (course.IsDeleted)
                return new ServiceResponse { success = false, message = "Course is already deleted" };
            course.IsDeleted = true;
            course.DeletedAt = DateTime.UtcNow;
            course.DeletedById = deletedById;
            course.DeletedByName = deletedByName;
            var result = await CoursesManagment.UpdateAsync(course);
            if (result == null)
                return new ServiceResponse { success = false, message = "Failed to delete Course" };
            await activityLogService.LogAsync(deletedById, deletedByName, "CourseDeleted", "Course", course.Id.ToString(), $"{course.Name} was soft deleted");
            return new ServiceResponse { success = true, message = "Course deleted successfully" };
        }

        public async Task<ServiceResponse> RestoreCourse(Guid id, string restoredById, string restoredByName)
        {
            if (id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid Course ID" };

            var course = await CoursesManagment.GetByIdAsync(id);
            if (course == null)
                return new ServiceResponse { success = false, message = "Course not found" };

            if (!course.IsDeleted)
                return new ServiceResponse { success = false, message = "Course is not deleted" };

            course.IsDeleted = false;
            course.DeletedAt = null;
            course.DeletedById = null;
            course.DeletedByName = null;
            course.RestoredAt = DateTime.UtcNow;
            course.RestoredById = restoredById;
            course.RestoredByName = restoredByName;
            var result = await CoursesManagment.UpdateAsync(course);
            if (result == null)
                return new ServiceResponse { success = false, message = "Failed to restore Course" };

            await activityLogService.LogAsync(restoredById, restoredByName, "CourseRestored", "Course", course.Id.ToString(), $"{course.Name} was restored");
            return new ServiceResponse { success = true, message = "Course restored successfully" };
        }

        public async Task<ServiceResponse> AssignInstructor(Guid courseId, string instructorId, string currentUserId, bool isAdmin)
        {
            if (courseId == Guid.Empty || string.IsNullOrWhiteSpace(instructorId) || string.IsNullOrWhiteSpace(currentUserId))
                return new ServiceResponse(false, "Course id, instructor id, and current user are required");

            var course = await CoursesManagment.GetByIdAsync(courseId);
            if (course == null || course.IsDeleted)
                return new ServiceResponse(false, "Course not found");

            var instructor = await UserManagment.GetUserById(instructorId);
            if (instructor == null)
                return new ServiceResponse(false, "Instructor not found");

            if (!course.OrganizationId.HasValue ||
                !instructor.OrganizationId.HasValue ||
                course.OrganizationId.Value != instructor.OrganizationId.Value)
            {
                return new ServiceResponse(false, "Instructor must belong to the same organization as the course");
            }

            if (!isAdmin)
            {
                var currentUser = await UserManagment.GetUserById(currentUserId);
                if (currentUser?.OrganizationId.HasValue != true ||
                    course.OrganizationId != currentUser.OrganizationId ||
                    course.OrganizationId != currentUser.OrganizationId)
                {
                    return new ServiceResponse(false, "You can assign only instructors from your organization to your courses");
                }
            }

            course.InstructorId = instructorId;
            var result = await CoursesManagment.UpdateAsync(course);
            if (result == null)
                return new ServiceResponse(false, "Failed to assign instructor");

            var actor = await UserManagment.GetUserById(currentUserId);
            await activityLogService.LogAsync(currentUserId, DisplayName(actor), "CourseInstructorAssigned", "Course", course.Id.ToString(), $"{DisplayName(instructor)} assigned to {course.Name}");
            return new ServiceResponse(true, "Instructor assigned to course successfully");
        }

        public async Task<bool> CourseExists(Guid id)
        {
            if (id == Guid.Empty)
            {
                return false;
            }

            return await CoursesManagment.GetByIdAsync(id) != null;
        }

        public async Task<bool> IsCourseDeleted(Guid id)
        {
            if (id == Guid.Empty)
            {
                return false;
            }

            var course = await CoursesManagment.GetByIdAsync(id);
            return course?.IsDeleted == true;
        }

        public async Task<bool> CanManageCourse(Guid courseId, string userId)
        {
            if (courseId == Guid.Empty || string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var course = await CoursesManagment.GetByIdAsync(courseId);
            var user = await UserManagment.GetUserById(userId);
            return course != null &&
                !course.IsDeleted &&
                course.OrganizationId.HasValue &&
                user?.OrganizationId.HasValue == true &&
                course.OrganizationId.Value == user.OrganizationId.Value;
        }

        public async Task<bool> CanManageAssignedCourse(Guid courseId, string userId)
        {
            if (courseId == Guid.Empty || string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var course = await CoursesManagment.GetByIdAsync(courseId);
            if (course == null || course.IsDeleted)
            {
                return false;
            }

            if (course.InstructorId == userId)
            {
                return true;
            }

            return (await SessionManagment.GetAllAsync()).Any(s => s.CourseId == courseId && s.TrainerId == userId);
        }

        public async Task<bool> CanManageSession(Guid sessionId, string userId)
        {
            if (sessionId == Guid.Empty || string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var session = await SessionManagment.GetByIdAsync(sessionId);
            var course = session == null ? null : await CoursesManagment.GetByIdAsync(session.CourseId);
            return session != null &&
                (session.TrainerId == userId || course?.InstructorId == userId || await CanManageCourse(session.CourseId, userId));
        }

        public async Task<bool> CanManageAssignment(Guid assignmentId, string userId)
        {
            if (assignmentId == Guid.Empty || string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var assignment = await AssignmentManagment.GetByIdAsync(assignmentId);
            if (assignment == null)
            {
                return false;
            }

            var session = await SessionManagment.GetByIdAsync(assignment.SessionId);
            var course = session == null ? null : await CoursesManagment.GetByIdAsync(session.CourseId);
            return session != null &&
                (session.TrainerId == userId || course?.InstructorId == userId || await CanManageCourse(session.CourseId, userId));
        }

        private async Task<IQueryable<Course>> ScopeCoursesAsync(
            IQueryable<Course> sourceCourses,
            string? userId,
            bool isAdmin,
            bool isOrganizationAdmin,
            bool isInstructor)
        {
            if (isAdmin)
            {
                return sourceCourses;
            }

            if (isOrganizationAdmin)
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return sourceCourses.Where(_ => false);

                var user = await UserManagment.GetUserById(userId);
                if (user?.OrganizationId.HasValue != true)
                    return sourceCourses.Where(_ => false);

                return sourceCourses.Where(c => c.OrganizationId == user.OrganizationId.Value);
            }

            if (isInstructor)
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return sourceCourses.Where(_ => false);

                var instructor = await UserManagment.GetUserById(userId);
                if (instructor?.OrganizationId.HasValue != true)
                    return sourceCourses.Where(_ => false);

                var assignedCourseIds = SessionManagment.Query()
                    .Where(s => s.TrainerId == userId)
                    .Select(s => s.CourseId);

                return sourceCourses
                    .Where(c => c.OrganizationId == instructor.OrganizationId &&
                        (c.InstructorId == userId || assignedCourseIds.Contains(c.Id)));
            }

            return sourceCourses;
        }

        public async Task<List<GetCourse>> GetAllCourses(string? userid, bool isAdmin = false, bool isOrganizationAdmin = false, bool isInstructor = false, int page = 1, int pageSize = 50)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = await ScopeCoursesAsync(
                CoursesManagment.Query().Where(c => !c.IsDeleted),
                userid,
                isAdmin,
                isOrganizationAdmin,
                isInstructor);
            return await LoadCourseDtosAsync(query, userid, page, pageSize);
        }

        public async Task<List<GetCourse>> GetDeletedCourses(string? userid)
        {
            return await LoadCourseDtosAsync(
                CoursesManagment.Query().Where(c => c.IsDeleted),
                userid);
        }
        public async Task<List<GetCourse>> GetCourseByCategory(Guid categoryId, string? userid, bool isAdmin = false, bool isOrganizationAdmin = false, bool isInstructor = false)
        {
            var courseIds = CoursesCatManagment.Query()
                .Where(link => link.CategoryId == categoryId)
                .Select(link => link.CourseId);
            var query = await ScopeCoursesAsync(
                CoursesManagment.Query().Where(c => !c.IsDeleted && courseIds.Contains(c.Id)),
                userid,
                isAdmin,
                isOrganizationAdmin,
                isInstructor);
            return await LoadCourseDtosAsync(query, userid);
        }
        public async Task<List<GetCourse>> Search(string name,string? userid, bool isAdmin = false, bool isOrganizationAdmin = false, bool isInstructor = false)
        {
            var normalizedName = name?.Trim() ?? string.Empty;
            var query = CoursesManagment.Query().Where(c =>
                !c.IsDeleted &&
                (c.Name.Contains(normalizedName) || c.Title.Contains(normalizedName)));
            query = await ScopeCoursesAsync(query, userid, isAdmin, isOrganizationAdmin, isInstructor);
            return await LoadCourseDtosAsync(query, userid);
        }

        private async Task<List<GetCourse>> LoadCourseDtosAsync(
            IQueryable<Course> query,
            string? userId,
            int page = 1,
            int pageSize = 100)
        {
            var stopwatch = Stopwatch.StartNew();
            var mappedCourses = await query
                .OrderBy(course => course.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(course => new GetCourse
                {
                    Id = course.Id,
                    Name = course.Name,
                    Description = course.Description,
                    Title = course.Title,
                    Price = course.Price,
                    Duration = course.Duration,
                    ImageUrl = course.ImageUrl,
                    OrgId = course.OrgId,
                    OrganizationId = course.OrganizationId,
                    OrganizationName = OrganizationManagment.Query(false)
                        .Where(organization => course.OrganizationId.HasValue && organization.Id == course.OrganizationId.Value)
                        .Select(organization => organization.Name)
                        .FirstOrDefault() ?? "EduVerseOrganization",
                    OrganizationOwnerName = OrganizationManagment.Query(false)
                        .Where(organization => course.OrganizationId.HasValue && organization.Id == course.OrganizationId.Value)
                        .Select(organization => organization.Name)
                        .FirstOrDefault() ?? "EduVerseOrganization",
                    OrganizationOwnerEmail = OrganizationManagment.Query(false)
                        .Where(organization => course.OrganizationId.HasValue && organization.Id == course.OrganizationId.Value)
                        .Select(organization => organization.Email)
                        .FirstOrDefault(),
                    InstructorId = course.InstructorId,
                    InstructorName = UserManagment.QueryUsers(false)
                        .Where(instructor => instructor.Id ==
                            (course.InstructorId ?? SessionManagment.Query(false)
                                .Where(session => session.CourseId == course.Id && session.TrainerId != null)
                                .Select(session => session.TrainerId)
                                .FirstOrDefault()))
                        .Select(instructor => instructor.FullName ?? instructor.Email)
                        .FirstOrDefault(),
                    StudentsCount = EnrollmentManagment.Query(false)
                        .Where(enrollment => enrollment.CourseId == course.Id)
                        .Select(enrollment => enrollment.StudentId)
                        .Distinct()
                        .Count(),
                    SessionsCount = SessionManagment.Query(false).Count(session => session.CourseId == course.Id),
                    RatingCount = RatingManagment.Query(false).Count(rating => rating.CourseId == course.Id),
                    Rating = RatingManagment.Query(false)
                        .Where(rating => rating.CourseId == course.Id)
                        .Select(rating => (float?)rating.RatingValue)
                        .Average() ?? 0,
                    UserRating = string.IsNullOrWhiteSpace(userId)
                        ? 0
                        : RatingManagment.Query(false)
                            .Where(rating => rating.CourseId == course.Id && rating.StudentId == userId)
                            .Select(rating => rating.RatingValue)
                            .FirstOrDefault(),
                    IsDeleted = course.IsDeleted,
                    DeletedAt = course.DeletedAt,
                    DeletedById = course.DeletedById,
                    DeletedByName = course.DeletedByName,
                    RestoredAt = course.RestoredAt,
                    RestoredById = course.RestoredById,
                    RestoredByName = course.RestoredByName,
                    Tags = course.Tags,
                    Level = course.Level,
                    Categories = new List<GetCategory>()
                })
                .ToListAsync();

            if (mappedCourses.Count == 0)
                return [];

            var courseIds = mappedCourses.Select(course => course.Id).ToList();
            var categoryRows = await (
                from link in CoursesCatManagment.Query()
                join category in CategoryManagment.Query() on link.CategoryId equals category.Id
                where courseIds.Contains(link.CourseId)
                select new
                {
                    link.CourseId,
                    Category = new GetCategory
                    {
                        Id = category.Id,
                        Name = category.Name,
                        Description = category.Description
                    }
                })
                .ToListAsync();
            var categoriesByCourse = categoryRows
                .GroupBy(row => row.CourseId)
                .ToDictionary(group => group.Key, group => group.Select(row => row.Category).ToList());

            foreach (var course in mappedCourses)
            {
                course.Categories = categoriesByCourse.GetValueOrDefault(course.Id) ?? [];
                course.Category = course.Categories.FirstOrDefault()?.Name;
            }

            stopwatch.Stop();
            logger.LogInformation(
                "Course read returned {CourseCount} courses in {ElapsedMs}ms",
                mappedCourses.Count,
                stopwatch.ElapsedMilliseconds);
            return mappedCourses;
        }

        public async Task<GetCourse> GetCourseById(Guid id, string? userid)
        {
            return (await LoadCourseDtosAsync(
                CoursesManagment.Query().Where(course => course.Id == id && !course.IsDeleted),
                userid,
                1)).FirstOrDefault();
        }

        public async Task<GetCourse> GetCourseByName(string name, string? userid)
        {
            return (await LoadCourseDtosAsync(
                CoursesManagment.Query().Where(course => !course.IsDeleted && course.Name == name),
                userid,
                1)).FirstOrDefault();
        }

        public async Task<AdminCourseDetailsDto?> GetAdminCourseDetails(Guid id, string? currentUserId, bool isAdmin, bool isOrganizationAdmin, bool isInstructor)
        {
            if (id == Guid.Empty)
                return null;

            var course = await CoursesManagment.GetByIdAsync(id);
            if (course == null)
                return null;

            var sessions = (await SessionManagment.GetAllAsync())
                .Where(s => s.CourseId == course.Id)
                .OrderBy(s => s.SessionNumber)
                .ToList();

            if (!isAdmin)
            {
                if (string.IsNullOrWhiteSpace(currentUserId))
                    return null;

                if (isOrganizationAdmin)
                {
                    var currentUser = await UserManagment.GetUserById(currentUserId);
                    if (!course.OrganizationId.HasValue ||
                        currentUser?.OrganizationId.HasValue != true ||
                        course.OrganizationId.Value != currentUser.OrganizationId.Value)
                    {
                        return null;
                    }
                }

                if (isInstructor && course.InstructorId != currentUserId && !sessions.Any(s => s.TrainerId == currentUserId))
                    return null;
            }

            var categoryLinks = (await CoursesCatManagment.GetAllAsync())
                .Where(cc => cc.CourseId == course.Id)
                .ToList();
            var categories = new List<GetCategory>();
            foreach (var courseCategory in categoryLinks)
            {
                var category = await CategoryManagment.GetByIdAsync(courseCategory.CategoryId);
                if (category != null)
                {
                    categories.Add(mapper.Map<GetCategory>(category));
                }
            }

            var enrollments = (await EnrollmentManagment.GetAllAsync())
                .Where(e => e.CourseId == course.Id)
                .ToList();
            var ratings = (await RatingManagment.GetAllAsync())
                .Where(r => r.CourseId == course.Id)
                .ToList();
            var assignments = (await AssignmentManagment.GetAllAsync())
                .Where(a => sessions.Any(s => s.Id == a.SessionId))
                .ToList();

            var students = new List<AdminCourseStudentDto>();
            foreach (var enrollment in enrollments)
            {
                var student = await UserManagment.GetUserById(enrollment.StudentId);
                students.Add(new AdminCourseStudentDto
                {
                    StudentId = enrollment.StudentId,
                    StudentName = student?.FullName ?? string.Empty,
                    StudentEmail = student?.Email ?? string.Empty,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    Progression = enrollment.Progression
                });
            }

            var payments = new List<AdminPaymentTransactionDto>();
            foreach (var payment in (await GetCoursePayments(course.Id)).OrderByDescending(p => p.SubmittingDate).Take(5))
            {
                payments.Add(payment);
            }

            var organization = course.OrganizationId.HasValue ? await OrganizationManagment.GetByIdAsync(course.OrganizationId.Value) : null;
            var owner = !string.IsNullOrWhiteSpace(course.OrgId) ? await UserManagment.GetUserById(course.OrgId) : null;
            var trainerId = course.InstructorId ?? sessions.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.TrainerId))?.TrainerId;
            var trainer = !string.IsNullOrWhiteSpace(trainerId) ? await UserManagment.GetUserById(trainerId) : null;

            return new AdminCourseDetailsDto
            {
                CourseId = course.Id,
                Name = course.Name,
                Title = course.Title,
                Description = course.Description,
                Category = categories.FirstOrDefault()?.Name,
                OrganizationId = organization?.Id,
                OrganizationName = organization?.Name ?? "EduVerseOrganization",
                OrganizationOwner = organization?.Name ?? "EduVerseOrganization",
                OrganizationOwnerEmail = organization?.Email,
                InstructorId = trainer?.Id,
                InstructorName = trainer?.FullName,
                Price = course.Price,
                ImageUrl = course.ImageUrl,
                StudentsCount = students.Select(s => s.StudentId).Distinct().Count(),
                SessionsCount = sessions.Count,
                AverageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.RatingValue), 2) : 0,
                IsDeleted = course.IsDeleted,
                DeletedAt = course.DeletedAt,
                DeletedById = course.DeletedById,
                DeletedByName = course.DeletedByName,
                RestoredAt = course.RestoredAt,
                RestoredById = course.RestoredById,
                RestoredByName = course.RestoredByName,
                Sessions = mapper.Map<List<GetSession>>(sessions),
                Students = students,
                Assignments = mapper.Map<List<GetAssignment>>(assignments),
                RecentPayments = payments
            };
        }

        private async Task<List<AdminPaymentTransactionDto>> GetCoursePayments(Guid courseId)
        {
            var course = await CoursesManagment.GetByIdAsync(courseId);
            var payments = (await PaymentManagment.GetAllAsync())
                .Where(p => p.CourseId == courseId)
                .OrderByDescending(p => p.SubmittingDate)
                .ToList();
            var result = new List<AdminPaymentTransactionDto>();

            foreach (var payment in payments)
            {
                var student = await UserManagment.GetUserById(payment.StudentId);
                result.Add(new AdminPaymentTransactionDto
                {
                    CourseId = payment.CourseId,
                    CourseName = course?.Name ?? string.Empty,
                    StudentId = payment.StudentId,
                    StudentName = student?.FullName ?? string.Empty,
                    StudentEmail = student?.Email ?? string.Empty,
                    SubmittingDate = payment.SubmittingDate,
                    TotalPrice = payment.TotalPrice,
                    PaymentMethod = payment.PaymentMethod,
                    PaymentStatus = payment.PaymentStatus,
                    PaymentProvider = payment.PaymentProvider,
                    MerchantOrderId = payment.MerchantOrderId,
                    SpecialReference = payment.SpecialReference
                });
            }

            return result;
        }

        private async Task<ServiceResponse> UpdateDuration(Guid id, double duration)
        {
            if (id == Guid.Empty || duration <= 0)
                return new ServiceResponse { success = false, message = "Invalid Course ID or duration" };
            var course= await CoursesManagment.GetByIdAsync(id);
            if (course == null || course.IsDeleted)
                return new ServiceResponse { success = false, message = "Course not found" };
            course.Duration += duration;
            var result = await CoursesManagment.UpdateAsync(course);
            if (result == null)
                return new ServiceResponse { success = false, message = "Failed to update Course duration" };
            return new ServiceResponse { success = true, message = "Course duration updated successfully" };
        }

        public async Task<ServiceResponse> UpdateCourse(UpdateCourse Course)
        {
            if (Course == null || Course.Id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid Course data" };
            var existingCourse = await CoursesManagment.GetByIdAsync(Course.Id);
            if (existingCourse == null || existingCourse.IsDeleted)
                return new ServiceResponse { success = false, message = "Course not found" };
            existingCourse.Name = Course.Name;
            existingCourse.Description = Course.Description;
            existingCourse.Title = Course.Title;
            existingCourse.Price = Course.Price;
            existingCourse.Tags = Course.Tags;
            existingCourse.Level = Course.Level;

            if (Course.Image != null)
            {
                var fileDetails = new FileDetails { FileName = existingCourse.ImageUrl, Folder = "courses" };
                var cloudDeleteResult = await cloud.DeleteFileAsync(fileDetails);
                if (!cloudDeleteResult.success)
                    return new ServiceResponse { success = false, message = "Failed to delete old course image from cloud" };
                existingCourse.ImageUrl = $"{existingCourse.Id}-Thumbnail";
                var addCloudFile = new AddCloudFile { Details = fileDetails, File = Course.Image };
                var cloudUploadResult = await cloud.UploadFileAsync(addCloudFile);
                if (!cloudUploadResult.success)
                    return new ServiceResponse { success = false, message = "Failed to upload new course image to cloud" };
            }
            var categoryLinks = await CoursesCatManagment.GetAllAsync();
            var existingCourseCategories = categoryLinks.Where(cc => cc.CourseId == Course.Id).ToList();
            foreach (var courseCategory in existingCourseCategories)
            {
                await CoursesCatManagment.DeleteAsync(courseCategory);
            }
            var categoryIds = Course.Categories.Split(',').ToList();
            foreach (var category in categoryIds)
            {
                Guid categoryId;
                if (!Guid.TryParse(category, out categoryId))
                    continue;
                var test = await CategoryManagment.GetByIdAsync(categoryId);
                if (test == null)
                    continue;
                var courseCategory = new CourseCategory { CourseId = Course.Id, CategoryId = test.Id };
                await CoursesCatManagment.AddAsync(courseCategory);
            }
            var updateResult = await CoursesManagment.UpdateAsync(existingCourse);
            if (updateResult == null)
                return new ServiceResponse { success = false, message = "Failed to update Course" };
            await activityLogService.LogAsync(existingCourse.OrgId, "Organization admin", "CourseUpdated", "Course", existingCourse.Id.ToString(), $"{existingCourse.Name} was updated");
            return new ServiceResponse { success = true, message = "Course updated successfully" };
        }

        private static string DisplayName(AppUser? user)
        {
            return user?.FullName ?? user?.UserName ?? user?.Email ?? "Unknown user";
        }

        public async Task<ServiceResponse> AddSession(
            CreateSession session,
            string currentUserId,
            bool isAdmin,
            bool isOrganizationAdmin,
            bool isInstructor)
        {
            if (session == null)
                return new ServiceResponse { success = false, message = "Invalid session data" };
            if (session.CourseId == Guid.Empty)
                return new ServiceResponse { success = false, message = "CourseId is required" };
            if (string.IsNullOrWhiteSpace(currentUserId))
                return new ServiceResponse { success = false, message = "Authenticated user is required" };

            var course = await CoursesManagment.GetByIdAsync(session.CourseId);
            if (course == null || course.IsDeleted)
                return new ServiceResponse { success = false, message = "Course not found" };

            var currentUser = await UserManagment.GetUserById(currentUserId);
            if (currentUser == null)
                return new ServiceResponse { success = false, message = "Authenticated user was not found" };

            if (isInstructor)
            {
                if (!currentUser.OrganizationId.HasValue ||
                    !course.OrganizationId.HasValue ||
                    currentUser.OrganizationId.Value != course.OrganizationId.Value)
                {
                    return new ServiceResponse { success = false, message = "You can add sessions only to courses inside your organization" };
                }

                if (!string.Equals(course.InstructorId, currentUserId, StringComparison.Ordinal))
                    return new ServiceResponse { success = false, message = "You can add sessions only to courses assigned to you" };
            }
            else if (isOrganizationAdmin)
            {
                if (!currentUser.OrganizationId.HasValue ||
                    !course.OrganizationId.HasValue ||
                    currentUser.OrganizationId.Value != course.OrganizationId.Value)
                {
                    return new ServiceResponse { success = false, message = "You can add sessions only to courses inside your organization" };
                }
            }
            else if (!isAdmin)
            {
                return new ServiceResponse { success = false, message = "You are not allowed to add sessions" };
            }

            if (string.IsNullOrWhiteSpace(course.InstructorId))
                return new ServiceResponse { success = false, message = "Assign an instructor to the course before adding sessions" };

            var mappedSession = mapper.Map<Session>(session);
            mappedSession.TrainerId = course.InstructorId;
            mappedSession.Date= DateTime .Today;
            var duration = GetVideoDuration(session.File);
            mappedSession.Duration = duration.TotalMinutes;
            await UpdateDuration(mappedSession.CourseId, duration.TotalMinutes);
            if (session.File != null && session.File.Length > 0)
            {
                var fileDetails = new FileDetails { FileName = $"{mappedSession.Id}-SessionMaterial{Path.GetExtension(session.File.FileName)}", Folder = "sessions" };
                var addCloudFile = new AddCloudFile { Details = fileDetails, File = session.File };
                var uploadResult = await cloud.UploadFileAsync(addCloudFile);
                if (!uploadResult.success)
                    return new ServiceResponse { success = false, message = "Failed to upload session material to cloud" };
                mappedSession.FileUrl = fileDetails.FileName;
            }
            else
            {
                mappedSession.FileUrl = string.Empty;
            }
            var result = await SessionManagment.AddAsync(mappedSession);
            if (result == null)
                return new ServiceResponse { success = false, message = "Failed to add session" };

            var enrolled = (await EnrollmentManagment.GetAllAsync())
                .Where(enrollment => enrollment.CourseId == mappedSession.CourseId);
            var attendances = enrolled
                .Select(enrollment => new AttendanceRecord
                {
                    SessionId = mappedSession.Id,
                    StudentId = enrollment.StudentId,
                    Attended = false
                })
                .ToList();

            if (attendances.Count > 0)
                await attendencemanagment.MassAdd(attendances);

            return new ServiceResponse { success = true, message = "Session added successfully" };

        }

        public async Task<ServiceResponse> UpdateSession(UpdateSession session)
        {
            if (session == null || session.Id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid session data" };
            var existingSession = await SessionManagment.GetByIdAsync(session.Id);
            if (existingSession == null)
                return new ServiceResponse { success = false, message = "Session not found" };
            
            if(session.File != null)
            {
                var duration = GetVideoDuration(session.File);
                var durationDifference = (double)duration.TotalMinutes - existingSession.Duration;
                await UpdateDuration(existingSession.CourseId, (int)durationDifference);
                existingSession.Duration = duration.TotalMinutes;
                var fileDetails = new FileDetails { FileName = existingSession.FileUrl, Folder = "sessions" };
                var cloudDeleteResult = await cloud.DeleteFileAsync(fileDetails);
                if (!cloudDeleteResult.success)
                    return new ServiceResponse { success = false, message = "Failed to delete old session material from cloud" };
                var newFileDetails = new FileDetails { FileName = $"{existingSession.Id}-SessionMaterial{Path.GetExtension(session.File.FileName)}", Folder = "sessions" };
                var addCloudFile = new AddCloudFile { Details = newFileDetails, File = session.File };
                var cloudUploadResult = await cloud.UploadFileAsync(addCloudFile);
                if (!cloudUploadResult.success)
                    return new ServiceResponse { success = false, message = "Failed to upload new session material to cloud" };
                existingSession.FileUrl = newFileDetails.FileName;
            }
            existingSession.Title = session.Title;
            existingSession.TrainerId = session.TrainerId;
            existingSession.SessionNumber = session.SessionNumber;

            var updateResult = await SessionManagment.UpdateAsync(existingSession);
            if (updateResult == null)
                return new ServiceResponse { success = false, message = "Failed to update session" };

            return new ServiceResponse { success = true, message = "Session updated successfully" };

        }
        private TimeSpan GetVideoDuration(IFormFile videoFile)
        {
            // Ensure the file is a video and has content
            if (videoFile == null || videoFile.Length == 0)
            {
                return TimeSpan.Zero;
            }

            // Save the IFormFile to a temporary file path
            var tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                videoFile.CopyTo(stream);
            }

            try
            {
                var inputFile = new MediaFile { Filename = tempFilePath };
                using (var engine = new Engine())
                {
                    engine.GetMetadata(inputFile);
                }

                // The duration is available in the metadata
                return inputFile.Metadata.Duration;
            }
            catch (Exception ex)
            {
                
                return TimeSpan.Zero;
            }
            finally
            {
                // Clean up the temporary file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        public async Task<ServiceResponse> DeleteSession(Guid id)
        {
            if (id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid session ID" };
            var existingSession = await SessionManagment.GetByIdAsync(id);
            if (existingSession == null)
                return new ServiceResponse { success = false, message = "Session not found" };
            var result = await SessionManagment.DeleteAsync(existingSession);
            if (result == 0)
                return new ServiceResponse { success = false, message = "Failed to delete session" };
            var fileDetails = new FileDetails { FileName = existingSession.FileUrl, Folder = "sessions" };
            var cloudResult = await cloud.DeleteFileAsync(fileDetails);
            if (!cloudResult.success)
                return new ServiceResponse { success = false, message = "Failed to delete session material from cloud" };
            return new ServiceResponse { success = true, message = "Session deleted successfully" };

        }

        public async Task<List<GetSession>> GetCourseAllSessions(Guid courdeid, string? studentId = null)
        {
            if (courdeid == Guid.Empty)
                return new List<GetSession>();
            var course = await CoursesManagment.GetByIdAsync(courdeid);
            if (course == null || course.IsDeleted)
                return new List<GetSession>();
            var sessions = await SessionManagment.GetAllAsync();
            if (sessions == null || !sessions.Any())
                return new List<GetSession>();

            var courseSessions = sessions.Where(s => s.CourseId == courdeid).OrderBy(n=>n.SessionNumber).ToList();
            var mappedSessions = mapper.Map<List<GetSession>>(courseSessions);

            if (!string.IsNullOrWhiteSpace(studentId))
            {
                var isEnrolled = (await EnrollmentManagment.GetAllAsync())
                    .Any(e => e.CourseId == courdeid && e.StudentId == studentId);
                if (isEnrolled)
                {
                    var completedSessionIds = (await ProgressManagment.GetAllAsync())
                        .Where(p => p.CourseId == courdeid && p.StudentId == studentId && p.IsDone)
                        .Select(p => p.SessionId)
                        .ToHashSet();

                    foreach (var session in mappedSessions)
                    {
                        session.IsCompleted = completedSessionIds.Contains(session.Id);
                    }
                }
            }

            return mappedSessions;
        }

        public async Task<GetSession> GetSessionById(Guid id)
        {
            if (id == Guid.Empty)
                return null;
            var session = await SessionManagment.GetAllAsync();

            if (session == null||!session.Any())
                return null;
            var targetSession = session.FirstOrDefault(s => s.Id == id);
            if (targetSession == null)
                return null;
            var course = await CoursesManagment.GetByIdAsync(targetSession.CourseId);
            if (course == null || course.IsDeleted)
                return null;
            var mappedSession = mapper.Map<GetSession>(targetSession);
            return mappedSession;
        }

        public async Task<GetSession> GetSessionByNumber(Guid courseid, int sessionnumber)
        {
            if (courseid == Guid.Empty || sessionnumber <= 0)
                return null;
            var sessions = await SessionManagment.GetAllAsync();
            if (sessions == null || !sessions.Any())
                return null;
            var targetSession = sessions.FirstOrDefault(s => s.CourseId == courseid && s.SessionNumber == sessionnumber);
            if (targetSession == null)
                return null;
            var course = await CoursesManagment.GetByIdAsync(targetSession.CourseId);
            if (course == null || course.IsDeleted)
                return null;
            var mappedSession = mapper.Map<GetSession>(targetSession);
            return mappedSession;
        }

        public async Task<ServiceResponse> AddAssignment(CreateAssignment assignment)
        {
            if (assignment == null || assignment.SessionId == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid assignment data" };
            if (string.IsNullOrWhiteSpace(assignment.Subject) || string.IsNullOrWhiteSpace(assignment.Description))
                return new ServiceResponse { success = false, message = "Assignment subject and description are required" };
            if (await SessionManagment.GetByIdAsync(assignment.SessionId) == null)
                return new ServiceResponse { success = false, message = "Session not found" };

            var mappedAssignment = mapper.Map<Assignment>(assignment);
            string? uploadedFileName = null;
            if (assignment.File != null && assignment.File.Length > 0)
            {
                var fileDetails = new FileDetails { FileName = $"{mappedAssignment.Id}-AssignmentMaterial{Path.GetExtension(assignment.File.FileName)}", Folder = "assignments" };
                var addCloudFile = new AddCloudFile { Details = fileDetails, File = assignment.File };
                var uploadResult = await cloud.UploadFileAsync(addCloudFile);
                if (!uploadResult.success)
                    return new ServiceResponse { success = false, message = "Failed to upload assignment material to cloud" };
                mappedAssignment.Content = fileDetails.FileName;
                uploadedFileName = fileDetails.FileName;
            }
            else
            {
                mappedAssignment.Content = string.Empty;
            }

            try
            {
                var result = await AssignmentManagment.AddAsync(mappedAssignment);
                if (result == null)
                {
                    await DeleteUploadedAssignmentFile(uploadedFileName);
                    return new ServiceResponse { success = false, message = "Failed to add assignment" };
                }

                return new ServiceResponse { success = true, message = "Assignment added successfully", data = new { assignmentId = result.Id } };
            }
            catch
            {
                await DeleteUploadedAssignmentFile(uploadedFileName);
                return new ServiceResponse { success = false, message = "Failed to save assignment" };
            }
        }

        private async Task DeleteUploadedAssignmentFile(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            try
            {
                await cloud.DeleteFileAsync(new FileDetails { FileName = fileName, Folder = "assignments" });
            }
            catch
            {
                // The database operation already failed; cloud cleanup must not hide that response.
            }
        }

        public async Task<ServiceResponse> UpdateAssignment(UpdateAssignment assignment)
        {
            if (assignment == null || assignment.Id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid assignment data" };
            var existingAssignment = await AssignmentManagment.GetByIdAsync(assignment.Id);
            if (existingAssignment == null)
                return new ServiceResponse { success = false, message = "Assignment not found" };
            if (assignment.File != null)
            {
                var fileDetails = new FileDetails { FileName = existingAssignment.Content, Folder = "assignments" };
                var cloudDeleteResult = await cloud.DeleteFileAsync(fileDetails);
                if (!cloudDeleteResult.success)
                    return new ServiceResponse { success = false, message = "Failed to delete old assignment material from cloud" };
                var newFileDetails = new FileDetails { FileName = $"{existingAssignment.Id}-AssignmentMaterial{Path.GetExtension(assignment.File.FileName)}", Folder = "assignments" };
                var addCloudFile = new AddCloudFile { Details = newFileDetails, File = assignment.File };
                var cloudUploadResult = await cloud.UploadFileAsync(addCloudFile);
                if (!cloudUploadResult.success)
                    return new ServiceResponse { success = false, message = "Failed to upload new assignment material to cloud" };
                existingAssignment.Content = newFileDetails.FileName;
            }
            existingAssignment.Subject = assignment.Subject;
            existingAssignment.Description = assignment.Description;
            existingAssignment.DueDate = assignment.DueDate;
            var updateResult = await AssignmentManagment.UpdateAsync(existingAssignment);
            if (updateResult == null)
                return new ServiceResponse { success = false, message = "Failed to update assignment" };
            return new ServiceResponse { success = true, message = "Assignment updated successfully" };

        }

        public async Task<ServiceResponse> DeleteAssignment(Guid id)
        {
            if (id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid assignment ID" };
            var existingAssignment = await AssignmentManagment.GetByIdAsync(id);
            if (existingAssignment == null)
                return new ServiceResponse { success = false, message = "Assignment not found" };
            var result = await AssignmentManagment.DeleteAsync(existingAssignment);
            if (result == 0)
                return new ServiceResponse { success = false, message = "Failed to delete assignment" };
            var fileDetails = new FileDetails { FileName = existingAssignment.Content, Folder = "assignments" };
            var cloudResult = await cloud.DeleteFileAsync(fileDetails);
            if (!cloudResult.success)
                return new ServiceResponse { success = false, message = "Failed to delete assignment material from cloud" };
            return new ServiceResponse { success = true, message = "Assignment deleted successfully" };
        }

        public async Task<List<GetAssignment>> GetCourseAllAssignments(Guid courdeid)
        {
            var assignments = await AssignmentManagment.GetAllAsync();
            if (assignments == null || !assignments.Any())
                return new List<GetAssignment>();
            var sessions = await GetCourseAllSessions(courdeid);
            var courseAssignments = assignments.Where(a => sessions.Any(s => s.Id == a.SessionId)).ToList();
            var mappedAssignments = mapper.Map<List<GetAssignment>>(courseAssignments);
            return mappedAssignments;


        }

        public async Task<GetAssignment> GetAssignmentById(Guid id)
        {
            if (id == Guid.Empty)
                return null;
            var assignment = await AssignmentManagment.GetByIdAsync(id);
            if (assignment == null)
                return null;
            var mappedAssignment = mapper.Map<GetAssignment>(assignment);
            return mappedAssignment;
        }

        public async Task<GetAssignment> GetAssignmentBySession(Guid sessionid)
        {
            if (sessionid == Guid.Empty)
                return null;
            var assignments = await AssignmentManagment.GetAllAsync();
            if (assignments == null || !assignments.Any())
                return null;
            var targetAssignment = assignments.FirstOrDefault(a => a.SessionId == sessionid);
            if (targetAssignment == null)
                return null;
            var mappedAssignment = mapper.Map<GetAssignment>(targetAssignment);
            return mappedAssignment;
        }

        private static bool IsEnrollmentCompleted(Enrollment enrollment)
        {
            return enrollment.IsCompleted
                || enrollment.CompletedAt.HasValue
                || enrollment.GraduationDate.HasValue
                || enrollment.Progression >= 100
                || enrollment.ProgressPercentage >= 100;
        }
    }
}
