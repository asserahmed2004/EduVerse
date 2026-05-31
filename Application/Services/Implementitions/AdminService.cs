using Application.DTOs.Admin;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

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
        IActivityLogService activityLogService) : IAdminService
    {
        public async Task<ServiceResponse> GlobalSearchAsync(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ServiceResponse(true, "Global search retrieved successfully", new GlobalSearchResultDto());

            var search = query.Trim().ToLower();
            var users = (await userManagement.GetAllUsers()).ToList();
            var courses = (await coursesManagement.GetAllAsync()).ToList();
            var categories = (await categoriesManagement.GetAllAsync()).ToDictionary(c => c.Id, c => c.Name);
            var courseCategories = (await courseCategoriesManagement.GetAllAsync()).ToList();

            var matchedUsers = new List<SearchUserDto>();
            var matchedOrganizations = new List<SearchOrganizationDto>();
            foreach (var user in users)
            {
                var role = string.IsNullOrWhiteSpace(user.Email) ? string.Empty : await roleManagement.GetUserRole(user.Email);
                var isMatch = Contains(user.FullName, search) || Contains(user.Email, search) || Contains(user.UserName, search);
                if (isMatch)
                {
                    matchedUsers.Add(new SearchUserDto
                    {
                        UserId = user.Id,
                        FullName = user.FullName,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        Role = role
                    });
                }

                if (string.Equals(role, "organizationAdmin", StringComparison.OrdinalIgnoreCase) && isMatch)
                {
                    matchedOrganizations.Add(new SearchOrganizationDto
                    {
                        OrganizationAdminId = user.Id,
                        OrganizationAdminName = user.FullName,
                        Email = user.Email ?? string.Empty
                    });
                }
            }

            var matchedCourses = courses
                .Where(course =>
                {
                    var courseCategoryNames = courseCategories
                        .Where(cc => cc.CourseId == course.Id && categories.ContainsKey(cc.CategoryId))
                        .Select(cc => categories[cc.CategoryId]);
                    return Contains(course.Name, search) ||
                           Contains(course.Title, search) ||
                           courseCategoryNames.Any(name => Contains(name, search));
                })
                .Take(8)
                .Select(course =>
                {
                    var category = courseCategories
                        .Where(cc => cc.CourseId == course.Id && categories.ContainsKey(cc.CategoryId))
                        .Select(cc => categories[cc.CategoryId])
                        .FirstOrDefault() ?? string.Empty;
                    return new SearchCourseDto
                    {
                        CourseId = course.Id,
                        Name = course.Name,
                        Title = course.Title,
                        Category = category,
                        IsDeleted = course.IsDeleted
                    };
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
            var activeCourses = (await coursesManagement.GetAllAsync()).Where(c => !c.IsDeleted).ToList();
            var activeCourseIds = activeCourses.Select(c => c.Id).ToHashSet();
            var sessions = (await sessionsManagement.GetAllAsync()).Where(s => activeCourseIds.Contains(s.CourseId)).ToList();
            var enrollments = (await enrollmentsManagement.GetAllAsync()).Where(e => activeCourseIds.Contains(e.CourseId)).ToList();

            var details = new UserActivityDetailsDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = role,
                Phone = user.PhoneNumber,
                CoursesCount = activeCourses.Count(c => c.OrgId == user.Id),
                SessionsCount = sessions.Count(s => s.TrainerId == user.Id),
                EnrollmentsCount = enrollments.Count(e => e.StudentId == user.Id),
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
