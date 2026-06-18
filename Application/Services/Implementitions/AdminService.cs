using Application.DTOs.Admin;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implementitions
{
    public class AdminService(
        IUserManagment userManagement,
        IRoleManagment roleManagement,
        IGeneric<Course> coursesManagement,
        IGeneric<CourseCategory> courseCategoriesManagement,
        IGeneric<Category> categoriesManagement,
        IGeneric<Enrollment> enrollmentsManagement,
        IGeneric<Session> sessionsManagement,
        IGeneric<Organization> organizationsManagement,
        IActivityLogService activityLogService) : IAdminService
    {
        public async Task<ServiceResponse> GlobalSearchAsync(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ServiceResponse(true, "Global search retrieved successfully", new GlobalSearchResultDto());

            var search = query.Trim();
            var users = await userManagement.QueryUsers()
                .Where(user =>
                    (user.FullName != null && user.FullName.Contains(search)) ||
                    (user.Email != null && user.Email.Contains(search)) ||
                    (user.UserName != null && user.UserName.Contains(search)))
                .Take(8)
                .ToListAsync();
            var organizations = await organizationsManagement.Query()
                .Where(organization =>
                    organization.Name.Contains(search) ||
                    (organization.Email != null && organization.Email.Contains(search)) ||
                    (organization.WebsiteUrl != null && organization.WebsiteUrl.Contains(search)))
                .OrderBy(organization => organization.Name)
                .Take(8)
                .ToListAsync();
            var matchedCourses = await (
                from course in coursesManagement.Query()
                where course.Name.Contains(search) ||
                      course.Title.Contains(search) ||
                    (from link in courseCategoriesManagement.Query(false)
                       join category in categoriesManagement.Query(false) on link.CategoryId equals category.Id
                       where link.CourseId == course.Id && category.Name.Contains(search)
                       select link.CourseId).Any()
                select new SearchCourseDto
                {
                    CourseId = course.Id,
                    Name = course.Name,
                    Title = course.Title,
                    Category = (from link in courseCategoriesManagement.Query(false)
                                join category in categoriesManagement.Query(false) on link.CategoryId equals category.Id
                                where link.CourseId == course.Id
                                select category.Name).FirstOrDefault() ?? string.Empty,
                    IsDeleted = course.IsDeleted
                })
                .Take(8)
                .ToListAsync();
            var organizationIds = users
                .Where(user => user.OrganizationId.HasValue)
                .Select(user => user.OrganizationId!.Value)
                .Distinct()
                .ToList();
            var userOrganizations = await organizationsManagement.Query()
                .Where(organization => organizationIds.Contains(organization.Id))
                .ToDictionaryAsync(organization => organization.Id);

            var matchedUsers = new List<SearchUserDto>();
            foreach (var user in users)
            {
                var role = string.IsNullOrWhiteSpace(user.Email) ? string.Empty : await roleManagement.GetUserRole(user.Email);
                userOrganizations.TryGetValue(user.OrganizationId ?? Guid.Empty, out var organization);
                matchedUsers.Add(new SearchUserDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Role = role,
                    OrganizationId = user.OrganizationId,
                    OrganizationName = organization?.Name ?? "EduVerseOrganization"
                });
            }

            var matchedOrganizations = organizations
                .Where(organization =>
                    Contains(organization.Name, search) ||
                    Contains(organization.Email, search) ||
                    Contains(organization.WebsiteUrl, search))
                .Take(8)
                .Select(organization => new SearchOrganizationDto
                {
                    OrganizationAdminId = organization.Id.ToString(),
                    OrganizationAdminName = organization.Name,
                    Email = organization.Email ?? string.Empty
                })
                .ToList();

            var result = new GlobalSearchResultDto
            {
                Users = matchedUsers.Take(8).ToList(),
                Organizations = matchedOrganizations.Take(8).ToList(),
                Courses = matchedCourses
            };

            return new ServiceResponse(true, "Global search retrieved successfully", result);
        }

        public async Task<ServiceResponse> GetUserDetailsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new ServiceResponse(false, "User id is required");

            var user = await userManagement.GetUserById(userId);
            if (user == null)
                return new ServiceResponse(false, "User not found");

            var role = string.IsNullOrWhiteSpace(user.Email) ? string.Empty : await roleManagement.GetUserRole(user.Email);
            var organization = user.OrganizationId.HasValue ? await organizationsManagement.GetByIdAsync(user.OrganizationId.Value) : null;
            var activeCourses = coursesManagement.Query().Where(c => !c.IsDeleted);
            var coursesCount = user.OrganizationId.HasValue
                ? await activeCourses.CountAsync(c => c.OrganizationId == user.OrganizationId.Value)
                : 0;
            var sessionsCount = await sessionsManagement.Query()
                .CountAsync(session => session.TrainerId == user.Id &&
                    activeCourses.Select(course => course.Id).Contains(session.CourseId));
            var enrollmentsCount = await enrollmentsManagement.Query()
                .CountAsync(enrollment => enrollment.StudentId == user.Id &&
                    activeCourses.Select(course => course.Id).Contains(enrollment.CourseId));

            var details = new UserActivityDetailsDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = role,
                OrganizationId = user.OrganizationId,
                OrganizationName = organization?.Name ?? "EduVerseOrganization",
                Phone = user.PhoneNumber,
                CoursesCount = coursesCount,
                SessionsCount = sessionsCount,
                EnrollmentsCount = enrollmentsCount,
                RecentActivityLogs = await activityLogService.GetByUserAsync(user.Id, 5)
            };

            return new ServiceResponse(true, "User details retrieved successfully", details);
        }

        private static bool Contains(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) && value.ToLower().Contains(search);
        }
    }
}
