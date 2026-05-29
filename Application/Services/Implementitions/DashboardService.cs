using Application.DTOs.Dashboard;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services.Implementitions
{
    public class DashboardService(
        IGeneric<Course> coursesManagement,
        IGeneric<Enrollment> enrollmentsManagement,
        IGeneric<Session> sessionsManagement,
        IGeneric<Assignment> assignmentsManagement,
        IGeneric<Payment> paymentsManagement,
        IGeneric<Rating> ratingsManagement,
        IUserManagment userManagement,
        IRoleManagment roleManagement) : IDashboardService
    {
        public async Task<ServiceResponse> GetOrganizationStatsAsync(string currentUserId, bool isAdmin)
        {
            if (!isAdmin && string.IsNullOrWhiteSpace(currentUserId))
            {
                return new ServiceResponse(false, "User id claim is missing");
            }

            var allCourses = (await coursesManagement.GetAllAsync()).ToList();
            var scopedCourses = isAdmin
                ? allCourses
                : allCourses.Where(c => c.OrgId == currentUserId).ToList();

            var activeCourses = scopedCourses.Where(c => !c.IsDeleted).ToList();
            var deletedCourses = scopedCourses.Count(c => c.IsDeleted);
            var activeCourseIds = activeCourses.Select(c => c.Id).ToHashSet();

            var sessions = (await sessionsManagement.GetAllAsync())
                .Where(s => activeCourseIds.Contains(s.CourseId))
                .ToList();

            var sessionIds = sessions.Select(s => s.Id).ToHashSet();

            var assignments = (await assignmentsManagement.GetAllAsync())
                .Where(a => sessionIds.Contains(a.SessionId))
                .ToList();

            var enrollments = (await enrollmentsManagement.GetAllAsync())
                .Where(e => activeCourseIds.Contains(e.CourseId))
                .ToList();

            var payments = (await paymentsManagement.GetAllAsync())
                .Where(p => activeCourseIds.Contains(p.CourseId))
                .ToList();

            var ratings = (await ratingsManagement.GetAllAsync())
                .Where(r => activeCourseIds.Contains(r.CourseId))
                .ToList();

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

            var stats = new OrganizationStatsDto
            {
                TotalCourses = activeCourses.Count,
                DeletedCourses = deletedCourses,
                TotalInstructors = totalInstructors,
                TotalStudents = totalStudents,
                TotalEnrollments = enrollments.Count,
                TotalSessions = sessions.Count,
                TotalAssignments = assignments.Count,
                TotalPayments = payments.Count,
                TotalRevenue = paidPayments.Sum(p => p.TotalPrice),
                AverageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.RatingValue), 2) : 0
            };

            return new ServiceResponse(true, "Organization stats retrieved successfully", stats);
        }

        private async Task<int> CountUsersInRoleAsync(string roleName)
        {
            var users = await userManagement.GetAllUsers();
            var count = 0;

            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    continue;
                }

                var role = await roleManagement.GetUserRole(user.Email);
                if (string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
