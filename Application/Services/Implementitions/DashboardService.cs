using Application.DTOs.Dashboard;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implementitions
{
    public class DashboardService(
        IGeneric<Course> coursesManagement,
        IGeneric<Enrollment> enrollmentsManagement,
        IGeneric<Session> sessionsManagement,
        IGeneric<Assignment> assignmentsManagement,
        IGeneric<Payment> paymentsManagement,
        IGeneric<Rating> ratingsManagement,
        IGeneric<Organization> organizationsManagement,
        IUserManagment userManagement,
        IRoleManagment roleManagement,
        IActivityLogService activityLogService) : IDashboardService
    {
        public async Task<ServiceResponse> GetOrganizationStatsAsync(
            string currentUserId,
            bool isAdmin,
            bool isOrganizationAdmin,
            bool isInstructor)
        {
            if (!isAdmin && string.IsNullOrWhiteSpace(currentUserId))
            {
                return new ServiceResponse(false, "User id claim is missing");
            }

            if (isAdmin)
            {
                var adminStats = await BuildAdminStatsAsync();
                return new ServiceResponse(true, "Organization stats retrieved successfully", adminStats);
            }

            var allCourses = await coursesManagement.Query().ToListAsync();
            var allSessions = await sessionsManagement.Query().ToListAsync();

            var scopedCourses = await ResolveScopedCourses(
                allCourses,
                allSessions,
                currentUserId,
                isAdmin,
                isOrganizationAdmin,
                isInstructor);

            var activeCourses = scopedCourses.Where(c => !c.IsDeleted).ToList();
            var deletedCourses = scopedCourses.Count(c => c.IsDeleted);
            var activeCourseIds = activeCourses.Select(c => c.Id).ToList();

            var sessions = allSessions
                .Where(s => activeCourseIds.Contains(s.CourseId))
                .ToList();

            if (isInstructor && !isAdmin && !isOrganizationAdmin)
            {
                sessions = sessions.Where(s =>
                    s.TrainerId == currentUserId ||
                    activeCourses.Any(c => c.Id == s.CourseId && c.InstructorId == currentUserId)).ToList();
            }

            var sessionIds = sessions.Select(s => s.Id).ToList();

            var assignments = await assignmentsManagement.Query()
                .Where(a => sessionIds.Contains(a.SessionId))
                .ToListAsync();

            var enrollments = await enrollmentsManagement.Query()
                .Where(e => activeCourseIds.Contains(e.CourseId))
                .ToListAsync();

            var payments = await paymentsManagement.Query()
                .Where(p => activeCourseIds.Contains(p.CourseId))
                .ToListAsync();

            var ratings = await ratingsManagement.Query()
                .Where(r => activeCourseIds.Contains(r.CourseId))
                .ToListAsync();

            var totalInstructors = isAdmin
                ? await CountUsersInRoleAsync("instructor")
                : sessions.Select(s => s.TrainerId)
                    .Append(currentUserId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .Count();

            var totalStudents = isAdmin
                ? await CountUsersInRoleAsync("student")
                : enrollments.Select(e => e.StudentId).Distinct().Count();

            var paidPayments = payments.Where(p =>
                string.Equals(p.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase));
            var pendingPayments = payments.Count(p =>
                string.Equals(p.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase));

            var stats = new OrganizationStatsDto
            {
                TotalUsers = isAdmin ? await userManagement.QueryUsers().CountAsync() : 0,
                TotalOrganizations = isAdmin ? await organizationsManagement.Query().CountAsync() : isOrganizationAdmin ? 1 : 0,
                TotalCourses = activeCourses.Count,
                DeletedCourses = deletedCourses,
                TotalInstructors = totalInstructors,
                TotalStudents = totalStudents,
                TotalEnrollments = enrollments.Count,
                TotalSessions = sessions.Count,
                TotalAssignments = assignments.Count,
                TotalPayments = payments.Count,
                PendingPayments = pendingPayments,
                TotalRevenue = paidPayments.Sum(p => p.TotalPrice),
                AverageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.RatingValue), 2) : 0
            };

            return new ServiceResponse(true, "Organization stats retrieved successfully", stats);
        }

        public async Task<ServiceResponse> GetOrganizationsOverviewAsync()
        {
            var organizations = await BuildOrganizationsOverviewAsync();
            return new ServiceResponse(true, "Organizations overview retrieved successfully", organizations);
        }

        public async Task<ServiceResponse> GetRecentEnrollmentsAsync()
        {
            var recentEnrollments = await (
                from enrollment in enrollmentsManagement.Query()
                join course in coursesManagement.Query().Where(c => !c.IsDeleted)
                    on enrollment.CourseId equals course.Id
                join student in userManagement.QueryUsers()
                    on enrollment.StudentId equals student.Id
                select new RecentEnrollmentDto
                {
                    CourseId = enrollment.CourseId,
                    CourseName = course.Name,
                    StudentId = enrollment.StudentId,
                    StudentName = student.FullName ?? string.Empty,
                    StudentEmail = student.Email ?? string.Empty,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    Progression = enrollment.Progression
                })
                .OrderByDescending(e => e.EnrollmentDate)
                .Take(5)
                .ToListAsync();

            return new ServiceResponse(true, "Recent enrollments retrieved successfully", recentEnrollments);
        }

        public async Task<ServiceResponse> GetRecentPaymentsAsync()
        {
            var recentPayments = await (
                from payment in paymentsManagement.Query()
                join course in coursesManagement.Query().Where(c => !c.IsDeleted)
                    on payment.CourseId equals course.Id
                join student in userManagement.QueryUsers()
                    on payment.StudentId equals student.Id
                select new RecentPaymentDto
                {
                    CourseId = payment.CourseId,
                    CourseName = course.Name,
                    StudentId = payment.StudentId,
                    StudentName = student.FullName ?? string.Empty,
                    StudentEmail = student.Email ?? string.Empty,
                    SubmittingDate = payment.SubmittingDate,
                    TotalPrice = payment.TotalPrice,
                    PaymentStatus = payment.PaymentStatus,
                    MerchantOrderId = payment.MerchantOrderId,
                    SpecialReference = payment.SpecialReference
                })
                .OrderByDescending(p => p.SubmittingDate)
                .Take(5)
                .ToListAsync();

            return new ServiceResponse(true, "Recent payments retrieved successfully", recentPayments);
        }

        public async Task<ServiceResponse> GetRecentCoursesAsync()
        {
            var courses = await coursesManagement.Query()
                .Where(c => !c.IsDeleted)
                .Take(5)
                .ToListAsync();

            var result = new List<RecentCourseDto>();
            foreach (var course in courses)
            {
                var organization = course.OrganizationId.HasValue ? await organizationsManagement.GetByIdAsync(course.OrganizationId.Value) : null;
                var owner = string.IsNullOrWhiteSpace(course.OrgId) ? null : await userManagement.GetUserById(course.OrgId);
                result.Add(new RecentCourseDto
                {
                    CourseId = course.Id,
                    CourseName = course.Name,
                    Title = course.Title,
                    OrganizationAdminId = organization?.Id.ToString() ?? course.OrgId,
                    OrganizationAdminName = organization?.Name ?? owner?.FullName ?? "Not assigned",
                    Price = course.Price,
                    IsDeleted = course.IsDeleted
                });
            }

            return new ServiceResponse(true, "Recent courses retrieved successfully", result);
        }

        public async Task<ServiceResponse> GetTopCoursesAsync()
        {
            var courses = await coursesManagement.Query()
                .Where(c => !c.IsDeleted)
                .ToListAsync();
            var courseIds = courses.Select(c => c.Id).ToList();
            var enrollments = await enrollmentsManagement.Query()
                .Where(e => courseIds.Contains(e.CourseId))
                .ToListAsync();
            var sessions = await sessionsManagement.Query()
                .Where(s => courseIds.Contains(s.CourseId))
                .ToListAsync();
            var payments = await paymentsManagement.Query()
                .Where(p => courseIds.Contains(p.CourseId))
                .ToListAsync();
            var ratings = await ratingsManagement.Query()
                .Where(r => courseIds.Contains(r.CourseId))
                .ToListAsync();

            var result = new List<TopCourseDto>();
            foreach (var course in courses)
            {
                var organization = course.OrganizationId.HasValue ? await organizationsManagement.GetByIdAsync(course.OrganizationId.Value) : null;
                var owner = string.IsNullOrWhiteSpace(course.OrgId) ? null : await userManagement.GetUserById(course.OrgId);
                var courseRatings = ratings.Where(r => r.CourseId == course.Id).ToList();
                result.Add(new TopCourseDto
                {
                    CourseId = course.Id,
                    CourseName = course.Name,
                    Title = course.Title,
                    OrganizationAdminName = organization?.Name ?? owner?.FullName ?? "Not assigned",
                    StudentsCount = enrollments.Where(e => e.CourseId == course.Id).Select(e => e.StudentId).Distinct().Count(),
                    SessionsCount = sessions.Count(s => s.CourseId == course.Id),
                    Revenue = payments.Where(p => p.CourseId == course.Id && IsPaid(p)).Sum(p => p.TotalPrice),
                    AverageRating = courseRatings.Any() ? Math.Round(courseRatings.Average(r => r.RatingValue), 2) : 0
                });
            }

            return new ServiceResponse(
                true,
                "Top courses retrieved successfully",
                result.OrderByDescending(c => c.Revenue).ThenByDescending(c => c.StudentsCount).Take(5).ToList());
        }

        public async Task<ServiceResponse> GetTopOrganizationsAsync()
        {
            var organizations = await BuildOrganizationsOverviewAsync();
            var result = organizations
                .OrderByDescending(o => o.Revenue)
                .ThenByDescending(o => o.EnrollmentsCount)
                .Take(5)
                .Select(o => new TopOrganizationDto
                {
                    OrganizationAdminId = o.OrganizationAdminId,
                    OrganizationAdminName = o.OrganizationAdminName,
                    Email = o.Email,
                    CoursesCount = o.CoursesCount,
                    EnrollmentsCount = o.EnrollmentsCount,
                    Revenue = o.Revenue,
                    AverageRating = o.AverageRating
                })
                .ToList();

            return new ServiceResponse(true, "Top organizations retrieved successfully", result);
        }

        public async Task<ServiceResponse> GetTopInstructorsAsync()
        {
            var activeCourseIds = await coursesManagement.Query()
                .Where(c => !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync();
            var sessions = await sessionsManagement.Query()
                .Where(s => activeCourseIds.Contains(s.CourseId) && !string.IsNullOrWhiteSpace(s.TrainerId))
                .ToListAsync();
            var enrollments = await enrollmentsManagement.Query()
                .Where(e => activeCourseIds.Contains(e.CourseId))
                .ToListAsync();
            var instructorIds = sessions.Select(s => s.TrainerId).Distinct().ToList();
            var instructors = await userManagement.QueryUsers()
                .Where(u => instructorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var result = new List<TopInstructorDto>();
            foreach (var group in sessions.GroupBy(s => s.TrainerId))
            {
                instructors.TryGetValue(group.Key, out var instructor);
                var instructorCourseIds = group.Select(s => s.CourseId).Distinct().ToHashSet();
                result.Add(new TopInstructorDto
                {
                    InstructorId = group.Key,
                    InstructorName = instructor?.FullName ?? string.Empty,
                    Email = instructor?.Email ?? string.Empty,
                    SessionsCount = group.Count(),
                    CoursesCount = instructorCourseIds.Count,
                    StudentsCount = enrollments
                        .Where(e => instructorCourseIds.Contains(e.CourseId))
                        .Select(e => e.StudentId)
                        .Distinct()
                        .Count()
                });
            }

            return new ServiceResponse(
                true,
                "Top instructors retrieved successfully",
                result.OrderByDescending(i => i.SessionsCount).ThenByDescending(i => i.StudentsCount).Take(5).ToList());
        }

        public async Task<ServiceResponse> GetOrganizationDetailsAsync(string organizationAdminId)
        {
            if (string.IsNullOrWhiteSpace(organizationAdminId))
            {
                return new ServiceResponse(false, "Organization id is required");
            }

            var organization = await ResolveOrganizationAsync(organizationAdminId);
            if (organization == null)
            {
                return new ServiceResponse(false, "Organization not found");
            }

            var courses = await coursesManagement.Query()
                .Where(c => !c.IsDeleted && c.OrganizationId == organization.Id)
                .ToListAsync();
            var courseIds = courses.Select(c => c.Id).ToList();
            var enrollments = await enrollmentsManagement.Query()
                .Where(e => courseIds.Contains(e.CourseId))
                .ToListAsync();
            var sessions = await sessionsManagement.Query()
                .Where(s => courseIds.Contains(s.CourseId))
                .ToListAsync();
            var payments = await paymentsManagement.Query()
                .Where(p => courseIds.Contains(p.CourseId))
                .ToListAsync();
            var ratings = await ratingsManagement.Query()
                .Where(r => courseIds.Contains(r.CourseId))
                .ToListAsync();

            var recentEnrollments = new List<RecentEnrollmentDto>();
            foreach (var enrollment in enrollments.OrderByDescending(e => e.EnrollmentDate).Take(5))
            {
                var course = courses.FirstOrDefault(c => c.Id == enrollment.CourseId);
                var student = await userManagement.GetUserById(enrollment.StudentId);
                recentEnrollments.Add(new RecentEnrollmentDto
                {
                    CourseId = enrollment.CourseId,
                    CourseName = course?.Name ?? string.Empty,
                    StudentId = enrollment.StudentId,
                    StudentName = student?.FullName ?? string.Empty,
                    StudentEmail = student?.Email ?? string.Empty,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    Progression = enrollment.Progression
                });
            }

            var recentPayments = new List<RecentPaymentDto>();
            foreach (var payment in payments.OrderByDescending(p => p.SubmittingDate).Take(5))
            {
                var course = courses.FirstOrDefault(c => c.Id == payment.CourseId);
                var student = await userManagement.GetUserById(payment.StudentId);
                recentPayments.Add(new RecentPaymentDto
                {
                    CourseId = payment.CourseId,
                    CourseName = course?.Name ?? string.Empty,
                    StudentId = payment.StudentId,
                    StudentName = student?.FullName ?? string.Empty,
                    StudentEmail = student?.Email ?? string.Empty,
                    SubmittingDate = payment.SubmittingDate,
                    TotalPrice = payment.TotalPrice,
                    PaymentStatus = payment.PaymentStatus,
                    MerchantOrderId = payment.MerchantOrderId,
                    SpecialReference = payment.SpecialReference
                });
            }

            var details = new OrganizationDetailsDto
            {
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                OrganizationAdminId = organization.Id.ToString(),
                OrganizationAdminName = organization.Name,
                Email = organization.Email ?? string.Empty,
                PhoneNumber = organization.PhoneNumber,
                Description = organization.Description,
                WebsiteUrl = organization.WebsiteUrl,
                Status = organization.Status,
                CoursesCount = courses.Count,
                StudentsCount = enrollments.Select(e => e.StudentId).Distinct().Count(),
                EnrollmentsCount = enrollments.Count,
                Revenue = payments.Where(IsPaid).Sum(p => p.TotalPrice),
                AverageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.RatingValue), 2) : 0,
                Courses = courses.Select(course =>
                {
                    var courseRatings = ratings.Where(r => r.CourseId == course.Id).ToList();
                    return new OrganizationCourseDto
                    {
                        CourseId = course.Id,
                        Name = course.Name,
                        Title = course.Title,
                        Price = course.Price,
                        StudentsCount = enrollments.Where(e => e.CourseId == course.Id).Select(e => e.StudentId).Distinct().Count(),
                        SessionsCount = sessions.Count(s => s.CourseId == course.Id),
                        AverageRating = courseRatings.Any() ? Math.Round(courseRatings.Average(r => r.RatingValue), 2) : 0
                    };
                }).ToList(),
                RecentEnrollments = recentEnrollments,
                RecentPayments = recentPayments
            };

            return new ServiceResponse(true, "Organization details retrieved successfully", details);
        }

        public async Task<ServiceResponse> GetRecentActivitiesAsync()
        {
            var activityLogs = await activityLogService.GetLatestAsync(5);
            var activities = activityLogs.Select(log => new RecentActivityDto
            {
                Type = log.Action,
                Title = log.Action,
                Description = log.Description,
                CreatedAt = log.CreatedAt
            }).ToList();

            return new ServiceResponse(
                true,
                "Recent activities retrieved successfully",
                activities);
        }

        public async Task<ServiceResponse> GetAdminStudentsAsync()
        {
            var users = await GetUsersInRoleAsync("student");
            return new ServiceResponse(true, "Students retrieved successfully", users);
        }

        public async Task<ServiceResponse> GetAdminInstructorsAsync()
        {
            var users = await GetUsersInRoleAsync("instructor");
            return new ServiceResponse(true, "Instructors retrieved successfully", users);
        }

        public async Task<ServiceResponse> GetRecentSessionsAsync()
        {
            var activeCourses = await coursesManagement.Query()
                .Where(c => !c.IsDeleted)
                .ToDictionaryAsync(c => c.Id);
            var activeCourseIds = activeCourses.Keys.ToList();
            var sessions = await sessionsManagement.Query()
                .Where(s => activeCourseIds.Contains(s.CourseId))
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.SessionNumber)
                .Take(10)
                .ToListAsync();

            var result = new List<AdminSessionDto>();
            foreach (var session in sessions)
            {
                activeCourses.TryGetValue(session.CourseId, out var course);
                var instructor = await userManagement.GetUserById(session.TrainerId);
                result.Add(new AdminSessionDto
                {
                    SessionId = session.Id,
                    CourseId = session.CourseId,
                    CourseName = course?.Name ?? string.Empty,
                    Title = session.Title,
                    InstructorName = DisplayName(instructor),
                    Date = session.Date,
                    SessionNumber = session.SessionNumber
                });
            }

            return new ServiceResponse(true, "Recent sessions retrieved successfully", result);
        }

        public async Task<ServiceResponse> GetRecentAssignmentsAsync()
        {
            var activeCourses = await coursesManagement.Query()
                .Where(c => !c.IsDeleted)
                .ToDictionaryAsync(c => c.Id);
            var activeCourseIds = activeCourses.Keys.ToList();
            var sessions = await sessionsManagement.Query()
                .Where(s => activeCourseIds.Contains(s.CourseId))
                .ToDictionaryAsync(s => s.Id);
            var sessionIds = sessions.Keys.ToList();
            var assignments = await assignmentsManagement.Query()
                .Where(a => sessionIds.Contains(a.SessionId))
                .Take(10)
                .ToListAsync();

            var result = assignments.Select(assignment =>
            {
                sessions.TryGetValue(assignment.SessionId, out var session);
                var courseName = session != null && activeCourses.TryGetValue(session.CourseId, out var course)
                    ? course.Name
                    : string.Empty;

                return new AdminAssignmentDto
                {
                    AssignmentId = assignment.Id,
                    SessionId = assignment.SessionId,
                    CourseName = courseName,
                    Subject = assignment.Subject,
                    Description = assignment.Description
                };
            }).ToList();

            return new ServiceResponse(true, "Recent assignments retrieved successfully", result);
        }

        public async Task<ServiceResponse> GetTopRatedCoursesAsync()
        {
            var courses = await coursesManagement.Query()
                .Where(c => !c.IsDeleted)
                .ToListAsync();
            var courseIds = courses.Select(c => c.Id).ToList();
            var ratings = await ratingsManagement.Query()
                .Where(r => courseIds.Contains(r.CourseId))
                .ToListAsync();
            var enrollments = await enrollmentsManagement.Query()
                .Where(e => courseIds.Contains(e.CourseId))
                .ToListAsync();
            var sessions = await sessionsManagement.Query()
                .Where(s => courseIds.Contains(s.CourseId))
                .ToListAsync();

            var result = new List<TopCourseDto>();
            foreach (var course in courses)
            {
                var courseRatings = ratings.Where(r => r.CourseId == course.Id).ToList();
                var organization = course.OrganizationId.HasValue ? await organizationsManagement.GetByIdAsync(course.OrganizationId.Value) : null;
                var owner = string.IsNullOrWhiteSpace(course.OrgId) ? null : await userManagement.GetUserById(course.OrgId);
                result.Add(new TopCourseDto
                {
                    CourseId = course.Id,
                    CourseName = course.Name,
                    Title = course.Title,
                    OrganizationAdminName = organization?.Name ?? owner?.FullName ?? "Not assigned",
                    StudentsCount = enrollments.Where(e => e.CourseId == course.Id).Select(e => e.StudentId).Distinct().Count(),
                    SessionsCount = sessions.Count(s => s.CourseId == course.Id),
                    Revenue = 0,
                    AverageRating = courseRatings.Any() ? Math.Round(courseRatings.Average(r => r.RatingValue), 2) : 0
                });
            }

            return new ServiceResponse(true, "Top rated courses retrieved successfully", result.OrderByDescending(c => c.AverageRating).Take(10).ToList());
        }

        public async Task<ServiceResponse> GetAdminUserDetailsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new ServiceResponse(false, "User id is required");
            }

            var user = await userManagement.GetUserById(userId);
            if (user == null)
            {
                return new ServiceResponse(false, "User not found");
            }

            var role = string.IsNullOrWhiteSpace(user.Email) ? string.Empty : await roleManagement.GetUserRole(user.Email);
            var activeCourses = await coursesManagement.Query().Where(c => !c.IsDeleted).ToListAsync();
            var activeCourseIds = activeCourses.Select(c => c.Id).ToList();
            var sessions = await sessionsManagement.Query().Where(s => activeCourseIds.Contains(s.CourseId)).ToListAsync();
            var enrollments = await enrollmentsManagement.Query().Where(e => activeCourseIds.Contains(e.CourseId)).ToListAsync();

            var details = new AdminUserDetailsDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = role,
                Phone = user.PhoneNumber,
                CoursesCount = user.OrganizationId.HasValue ? activeCourses.Count(c => c.OrganizationId == user.OrganizationId.Value) : 0,
                SessionsCount = sessions.Count(s => s.TrainerId == user.Id),
                EnrollmentsCount = enrollments.Count(e => e.StudentId == user.Id)
            };

            return new ServiceResponse(true, "User details retrieved successfully", details);
        }

        public async Task<ServiceResponse> GetRevenueTrendAsync(int days)
        {
            var normalizedDays = Math.Clamp(days, 7, 365);
            var fromDate = DateTime.UtcNow.Date.AddDays(-(normalizedDays - 1));
            var activeCourseIds = await coursesManagement.Query()
                .Where(c => !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync();
            var payments = await paymentsManagement.Query()
                .Where(p => activeCourseIds.Contains(p.CourseId) && p.PaymentStatus == "Paid" && p.SubmittingDate >= fromDate)
                .ToListAsync();

            var points = Enumerable.Range(0, normalizedDays)
                .Select(offset =>
                {
                    var date = fromDate.AddDays(offset);
                    return new TrendPointDto
                    {
                        Date = date,
                        Label = date.ToString("MMM dd"),
                        Value = payments.Where(p => p.SubmittingDate.Date == date).Sum(p => p.TotalPrice)
                    };
                })
                .ToList();

            return new ServiceResponse(true, "Revenue trend retrieved successfully", points);
        }

        public async Task<ServiceResponse> GetEnrollmentsTrendAsync(int days)
        {
            var normalizedDays = Math.Clamp(days, 7, 365);
            var fromDate = DateTime.UtcNow.Date.AddDays(-(normalizedDays - 1));
            var activeCourseIds = await coursesManagement.Query()
                .Where(c => !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync();
            var enrollments = await enrollmentsManagement.Query()
                .Where(e => activeCourseIds.Contains(e.CourseId) && e.EnrollmentDate >= fromDate)
                .ToListAsync();

            var points = Enumerable.Range(0, normalizedDays)
                .Select(offset =>
                {
                    var date = fromDate.AddDays(offset);
                    return new TrendPointDto
                    {
                        Date = date,
                        Label = date.ToString("MMM dd"),
                        Value = enrollments.Count(e => e.EnrollmentDate.Date == date)
                    };
                })
                .ToList();

            return new ServiceResponse(true, "Enrollments trend retrieved successfully", points);
        }

        public async Task<ServiceResponse> GetUsersByRoleAsync()
        {
            var roles = new[] { "admin", "organizationAdmin", "instructor", "student" };
            var result = new List<RoleCountDto>();
            foreach (var role in roles)
            {
                result.Add(new RoleCountDto
                {
                    Role = role,
                    Count = await CountUsersInRoleAsync(role)
                });
            }

            return new ServiceResponse(true, "Users by role retrieved successfully", result);
        }

        public async Task<ServiceResponse> GetTopCoursesChartAsync()
        {
            var courses = await coursesManagement.Query().Where(c => !c.IsDeleted).ToListAsync();
            var courseIds = courses.Select(c => c.Id).ToList();
            var enrollments = await enrollmentsManagement.Query().Where(e => courseIds.Contains(e.CourseId)).ToListAsync();
            var payments = await paymentsManagement.Query().Where(p => courseIds.Contains(p.CourseId) && p.PaymentStatus == "Paid").ToListAsync();
            var ratings = await ratingsManagement.Query().Where(r => courseIds.Contains(r.CourseId)).ToListAsync();

            var result = courses.Select(course =>
            {
                var courseRatings = ratings.Where(r => r.CourseId == course.Id).ToList();
                return new TopCourseChartDto
                {
                    CourseId = course.Id,
                    CourseName = course.Name,
                    Enrollments = enrollments.Count(e => e.CourseId == course.Id),
                    Revenue = payments.Where(p => p.CourseId == course.Id).Sum(p => p.TotalPrice),
                    AverageRating = courseRatings.Any() ? Math.Round(courseRatings.Average(r => r.RatingValue), 2) : 0
                };
            })
            .OrderByDescending(c => c.Enrollments)
            .ThenByDescending(c => c.Revenue)
            .Take(8)
            .ToList();

            return new ServiceResponse(true, "Top courses chart retrieved successfully", result);
        }

        private async Task<List<OrganizationOverviewDto>> BuildOrganizationsOverviewAsync()
        {
            var organizations = await organizationsManagement.Query().ToListAsync();
            var organizationAdmins = await roleManagement.GetUsersInRoleAsync("organizationAdmin");
            var courses = await coursesManagement.Query().ToListAsync();
            var activeCourses = courses.Where(c => !c.IsDeleted).ToList();
            var activeCourseIds = activeCourses.Select(c => c.Id).ToList();
            var enrollments = await enrollmentsManagement.Query()
                .Where(e => activeCourseIds.Contains(e.CourseId))
                .ToListAsync();
            var payments = await paymentsManagement.Query()
                .Where(p => activeCourseIds.Contains(p.CourseId))
                .ToListAsync();
            var ratings = await ratingsManagement.Query()
                .Where(r => activeCourseIds.Contains(r.CourseId))
                .ToListAsync();

            var result = new List<OrganizationOverviewDto>();
            foreach (var organization in organizations)
            {
                var primaryAdmin = organizationAdmins.FirstOrDefault(
                    user => user.OrganizationId == organization.Id);
                var organizationCourses = activeCourses.Where(c => c.OrganizationId == organization.Id).ToList();
                var organizationCourseIds = organizationCourses.Select(c => c.Id).ToHashSet();
                var organizationRatings = ratings.Where(r => organizationCourseIds.Contains(r.CourseId)).ToList();

                result.Add(new OrganizationOverviewDto
                {
                    OrganizationId = organization.Id,
                    OrganizationName = organization.Name,
                    OrganizationAdminId = organization.Id.ToString(),
                    OrganizationAdminName = organization.Name,
                    Email = organization.Email ?? primaryAdmin?.Email ?? string.Empty,
                    PhoneNumber = organization.PhoneNumber,
                    Description = organization.Description,
                    WebsiteUrl = organization.WebsiteUrl,
                    Status = organization.Status,
                    CoursesCount = organizationCourses.Count,
                    StudentsCount = enrollments
                        .Where(e => organizationCourseIds.Contains(e.CourseId))
                        .Select(e => e.StudentId)
                        .Distinct()
                        .Count(),
                    EnrollmentsCount = enrollments.Count(e => organizationCourseIds.Contains(e.CourseId)),
                    Revenue = payments
                        .Where(p => organizationCourseIds.Contains(p.CourseId) && IsPaid(p))
                        .Sum(p => p.TotalPrice),
                    AverageRating = organizationRatings.Any() ? Math.Round(organizationRatings.Average(r => r.RatingValue), 2) : 0
                });
            }

            return result;
        }

        private async Task<OrganizationStatsDto> BuildAdminStatsAsync()
        {
            var activeCourses = coursesManagement.Query().Where(course => !course.IsDeleted);
            var activeCourseIds = activeCourses.Select(course => course.Id);
            var activeSessions = sessionsManagement.Query()
                .Where(session => activeCourseIds.Contains(session.CourseId));
            var activeSessionIds = activeSessions.Select(session => session.Id);
            var activeEnrollments = enrollmentsManagement.Query()
                .Where(enrollment => activeCourseIds.Contains(enrollment.CourseId));
            var activePayments = paymentsManagement.Query()
                .Where(payment => activeCourseIds.Contains(payment.CourseId));
            var activeRatings = ratingsManagement.Query()
                .Where(rating => activeCourseIds.Contains(rating.CourseId));

            var ratingCount = await activeRatings.CountAsync();

            return new OrganizationStatsDto
            {
                TotalUsers = await userManagement.QueryUsers().AsNoTracking().CountAsync(),
                TotalOrganizations = await organizationsManagement.Query().CountAsync(),
                TotalCourses = await activeCourses.CountAsync(),
                DeletedCourses = await coursesManagement.Query().CountAsync(course => course.IsDeleted),
                TotalInstructors = await CountUsersInRoleAsync("instructor"),
                TotalStudents = await CountUsersInRoleAsync("student"),
                TotalEnrollments = await activeEnrollments.CountAsync(),
                TotalSessions = await activeSessions.CountAsync(),
                TotalAssignments = await assignmentsManagement.Query()
                    .CountAsync(assignment => activeSessionIds.Contains(assignment.SessionId)),
                TotalPayments = await activePayments.CountAsync(),
                PendingPayments = await activePayments.CountAsync(payment => payment.PaymentStatus == "Pending"),
                TotalRevenue = await activePayments
                    .Where(payment => payment.PaymentStatus == "Paid")
                    .SumAsync(payment => payment.TotalPrice),
                AverageRating = ratingCount == 0
                    ? 0
                    : Math.Round(await activeRatings.AverageAsync(rating => (double)rating.RatingValue), 2)
            };
        }

        private async Task<List<Course>> ResolveScopedCourses(
            List<Course> allCourses,
            List<Session> allSessions,
            string currentUserId,
            bool isAdmin,
            bool isOrganizationAdmin,
            bool isInstructor)
        {
            if (isAdmin)
            {
                return allCourses;
            }

            if (isOrganizationAdmin)
            {
                var user = await userManagement.GetUserById(currentUserId);
                if (user?.OrganizationId.HasValue != true)
                    return [];
                return allCourses.Where(c => c.OrganizationId == user.OrganizationId.Value).ToList();
            }

            if (isInstructor)
            {
                var assignedCourseIds = allSessions
                    .Where(s => s.TrainerId == currentUserId)
                    .Select(s => s.CourseId)
                    .ToHashSet();

                return allCourses.Where(c => c.InstructorId == currentUserId || assignedCourseIds.Contains(c.Id)).ToList();
            }

            return [];
        }

        private async Task<Organization?> ResolveOrganizationAsync(string organizationIdOrLegacyAdminId)
        {
            if (Guid.TryParse(organizationIdOrLegacyAdminId, out var organizationId))
            {
                var organization = await organizationsManagement.GetByIdAsync(organizationId);
                if (organization != null)
                    return organization;
            }

            var legacyUser = await userManagement.GetUserById(organizationIdOrLegacyAdminId);
            if (legacyUser?.OrganizationId.HasValue == true)
            {
                return await organizationsManagement.GetByIdAsync(legacyUser.OrganizationId.Value);
            }

            return null;
        }

        private async Task<int> CountUsersInRoleAsync(string roleName)
        {
            return await roleManagement.CountUsersInRoleAsync(roleName);
        }

        private async Task<List<AdminUserDetailsDto>> GetUsersInRoleAsync(string roleName)
        {
            var users = await roleManagement.GetUsersInRoleAsync(roleName);
            return users.Select(user => new AdminUserDetailsDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Role = roleName,
                    Phone = user.PhoneNumber
                })
                .ToList();
        }

        private static bool IsPaid(Payment payment)
        {
            return string.Equals(payment.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase);
        }

        private static string DisplayName(AppUser? user)
        {
            return user?.FullName ?? user?.UserName ?? user?.Email ?? "Unknown user";
        }
    }
}
