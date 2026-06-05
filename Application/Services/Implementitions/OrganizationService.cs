using Application.DTOs.Organization;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services.Implementitions
{
    public class OrganizationService(
        IGeneric<Organization> organizationsManagement,
        IGeneric<Course> coursesManagement,
        IGeneric<Enrollment> enrollmentsManagement,
        IGeneric<Payment> paymentsManagement,
        IGeneric<Rating> ratingsManagement,
        IUserManagment userManagement,
        IRoleManagment roleManagement,
        IActivityLogService activityLogService) : IOrganizationService
    {
        public async Task<ServiceResponse> GetAllAsync()
        {
            var organizations = (await organizationsManagement.GetAllAsync())
                .OrderBy(o => o.Name)
                .ToList();

            var result = new List<OrganizationDto>();
            foreach (var organization in organizations)
            {
                result.Add(await MapOrganizationAsync(organization, includeUsers: false));
            }

            return new ServiceResponse(true, "Organizations retrieved successfully", result);
        }

        public async Task<ServiceResponse> GetByIdAsync(Guid id, string? currentUserId, bool isAdmin, bool isOrganizationAdmin, bool isInstructor)
        {
            var organization = await organizationsManagement.GetByIdAsync(id);
            if (organization == null)
            {
                return new ServiceResponse(false, "Organization not found");
            }

            if (!isAdmin)
            {
                if (string.IsNullOrWhiteSpace(currentUserId))
                    return new ServiceResponse(false, "User id claim is missing");

                var currentUser = await userManagement.GetUserById(currentUserId);
                if (currentUser?.OrganizationId != organization.Id || (!isOrganizationAdmin && !isInstructor))
                    return new ServiceResponse(false, "You are not allowed to access this organization");
            }

            return new ServiceResponse(true, "Organization retrieved successfully", await MapOrganizationAsync(organization, includeUsers: true));
        }

        public async Task<ServiceResponse> CreateAsync(CreateOrganizationRequest request, string? createdById, string? createdByName)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return new ServiceResponse(false, "Organization name is required");
            }

            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                WebsiteUrl = request.WebsiteUrl,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                CreatedById = createdById,
                CreatedByName = string.IsNullOrWhiteSpace(createdByName) ? "Unknown" : createdByName
            };

            var result = await organizationsManagement.AddAsync(organization);
            await activityLogService.LogAsync(createdById, createdByName ?? "Unknown", "OrganizationCreated", "Organization", result.Id.ToString(), $"{result.Name} was created");

            return new ServiceResponse(true, "Organization created successfully", await MapOrganizationAsync(result, includeUsers: true));
        }

        public async Task<ServiceResponse> UpdateAsync(Guid id, UpdateOrganizationRequest request, string? updatedById, string? updatedByName)
        {
            var organization = await organizationsManagement.GetByIdAsync(id);
            if (organization == null)
                return new ServiceResponse(false, "Organization not found");

            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return new ServiceResponse(false, "Organization name is required");

            organization.Name = request.Name.Trim();
            organization.Description = request.Description;
            organization.Email = request.Email;
            organization.PhoneNumber = request.PhoneNumber;
            organization.WebsiteUrl = request.WebsiteUrl;
            organization.UpdatedAt = DateTime.UtcNow;

            var result = await organizationsManagement.UpdateAsync(organization);
            await activityLogService.LogAsync(updatedById, updatedByName ?? "Unknown", "OrganizationUpdated", "Organization", organization.Id.ToString(), $"{organization.Name} was updated");

            return new ServiceResponse(true, "Organization updated successfully", await MapOrganizationAsync(result, includeUsers: true));
        }

        public async Task<ServiceResponse> SuspendAsync(Guid id, string? userId, string? userName)
        {
            return await SetStatusAsync(id, "Suspended", "Organization suspended successfully", "OrganizationSuspended", userId, userName);
        }

        public async Task<ServiceResponse> ActivateAsync(Guid id, string? userId, string? userName)
        {
            return await SetStatusAsync(id, "Active", "Organization activated successfully", "OrganizationActivated", userId, userName);
        }

        public async Task<ServiceResponse> AssignAdminAsync(AssignOrganizationUserRequest request, string? performedById, string? performedByName)
        {
            return await AssignUserAsync(request, "organizationAdmin", "Organization admin assigned successfully", "OrganizationAdminAssigned", performedById, performedByName);
        }

        public async Task<ServiceResponse> AssignInstructorAsync(AssignOrganizationUserRequest request, string? performedById, string? performedByName)
        {
            return await AssignUserAsync(request, "instructor", "Instructor assigned to organization successfully", "InstructorAssignedToOrganization", performedById, performedByName);
        }

        private async Task<ServiceResponse> SetStatusAsync(Guid id, string status, string message, string action, string? userId, string? userName)
        {
            var organization = await organizationsManagement.GetByIdAsync(id);
            if (organization == null)
                return new ServiceResponse(false, "Organization not found");

            organization.Status = status;
            organization.UpdatedAt = DateTime.UtcNow;
            await organizationsManagement.UpdateAsync(organization);
            await activityLogService.LogAsync(userId, userName ?? "Unknown", action, "Organization", organization.Id.ToString(), $"{organization.Name} status changed to {status}");

            return new ServiceResponse(true, message, await MapOrganizationAsync(organization, includeUsers: true));
        }

        private async Task<ServiceResponse> AssignUserAsync(AssignOrganizationUserRequest request, string roleName, string message, string action, string? performedById, string? performedByName)
        {
            if (request == null || request.OrganizationId == Guid.Empty || string.IsNullOrWhiteSpace(request.UserId))
                return new ServiceResponse(false, "Organization id and user id are required");

            var organization = await organizationsManagement.GetByIdAsync(request.OrganizationId);
            if (organization == null)
                return new ServiceResponse(false, "Organization not found");

            var user = await userManagement.GetUserById(request.UserId);
            if (user == null)
                return new ServiceResponse(false, "User not found");

            var currentRole = await roleManagement.GetUserRole(user.Email);
            if (!string.Equals(currentRole, roleName, StringComparison.OrdinalIgnoreCase))
            {
                var assigned = await roleManagement.AddUserToRole(user, roleName);
                if (!assigned.Succeeded)
                    return new ServiceResponse(false, $"Failed to assign {roleName} role", null, assigned.Errors.Select(e => e.Description));
            }

            user.OrganizationId = organization.Id;
            var updateResult = await userManagement.UpdateUser(user);
            if (!updateResult.Succeeded)
            {
                return new ServiceResponse(false, "Failed to assign user to organization", null, updateResult.Errors.Select(e => e.Description));
            }

            await activityLogService.LogAsync(performedById, performedByName ?? "Unknown", action, "Organization", organization.Id.ToString(), $"{DisplayName(user)} assigned to {organization.Name}");
            return new ServiceResponse(true, message, await MapOrganizationAsync(organization, includeUsers: true));
        }

        private async Task<OrganizationDto> MapOrganizationAsync(Organization organization, bool includeUsers)
        {
            var activeCourses = (await coursesManagement.GetAllAsync())
                .Where(c => !c.IsDeleted && c.OrganizationId == organization.Id)
                .ToList();
            var activeCourseIds = activeCourses.Select(c => c.Id).ToHashSet();
            var enrollments = (await enrollmentsManagement.GetAllAsync())
                .Where(e => activeCourseIds.Contains(e.CourseId))
                .ToList();
            var payments = (await paymentsManagement.GetAllAsync())
                .Where(p => activeCourseIds.Contains(p.CourseId))
                .ToList();
            var ratings = (await ratingsManagement.GetAllAsync())
                .Where(r => activeCourseIds.Contains(r.CourseId))
                .ToList();

            var dto = new OrganizationDto
            {
                Id = organization.Id,
                Name = organization.Name,
                Description = organization.Description,
                Email = organization.Email,
                PhoneNumber = organization.PhoneNumber,
                LogoUrl = organization.LogoUrl,
                WebsiteUrl = organization.WebsiteUrl,
                Status = organization.Status,
                CreatedAt = organization.CreatedAt,
                UpdatedAt = organization.UpdatedAt,
                CreatedById = organization.CreatedById,
                CreatedByName = organization.CreatedByName,
                CoursesCount = activeCourses.Count,
                StudentsCount = enrollments.Select(e => e.StudentId).Distinct().Count(),
                EnrollmentsCount = enrollments.Count,
                Revenue = payments.Where(p => string.Equals(p.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)).Sum(p => p.TotalPrice),
                AverageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.RatingValue), 2) : 0
            };

            if (includeUsers)
            {
                var users = (await userManagement.GetAllUsers())
                    .Where(u => u.OrganizationId == organization.Id)
                    .ToList();

                var admins = new List<OrganizationUserDto>();
                var instructors = new List<OrganizationUserDto>();
                foreach (var user in users)
                {
                    var role = await roleManagement.GetUserRole(user.Email);
                    var mappedUser = new OrganizationUserDto
                    {
                        UserId = user.Id,
                        FullName = user.FullName,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        Role = role
                    };

                    if (string.Equals(role, "organizationAdmin", StringComparison.OrdinalIgnoreCase))
                        admins.Add(mappedUser);
                    if (string.Equals(role, "instructor", StringComparison.OrdinalIgnoreCase))
                        instructors.Add(mappedUser);
                }

                dto.Admins = admins;
                dto.Instructors = instructors;
            }

            return dto;
        }

        private static string DisplayName(AppUser user)
        {
            return user.FullName ?? user.UserName ?? user.Email ?? "Unknown";
        }
    }
}
