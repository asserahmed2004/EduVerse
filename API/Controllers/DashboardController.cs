using API.Authorization;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DashboardController(IDashboardService dashboardService) : ControllerBase
    {
        [HttpGet("OrganizationStats")]
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
        public async Task<IActionResult> GetOrganizationStats()
        {
            return await GetDashboardOverview();
        }

        [HttpGet("OrganizationOverview")]
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
        public async Task<IActionResult> GetOrganizationOverview()
        {
            return await GetDashboardOverview();
        }

        [HttpGet("OrganizationsOverview")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetOrganizationsOverview()
        {
            var result = await dashboardService.GetOrganizationsOverviewAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("RecentEnrollments")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetRecentEnrollments()
        {
            var result = await dashboardService.GetRecentEnrollmentsAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("RecentPayments")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetRecentPayments()
        {
            var result = await dashboardService.GetRecentPaymentsAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("RecentCourses")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetRecentCourses()
        {
            var result = await dashboardService.GetRecentCoursesAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("TopCourses")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetTopCourses()
        {
            var result = await dashboardService.GetTopCoursesAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("TopOrganizations")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetTopOrganizations()
        {
            var result = await dashboardService.GetTopOrganizationsAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("TopInstructors")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetTopInstructors()
        {
            var result = await dashboardService.GetTopInstructorsAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("OrganizationDetails/{organizationAdminId}")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetOrganizationDetails(string organizationAdminId)
        {
            var result = await dashboardService.GetOrganizationDetailsAsync(organizationAdminId);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("RecentActivities")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetRecentActivities()
        {
            var result = await dashboardService.GetRecentActivitiesAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("AdminStudents")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetAdminStudents()
        {
            var result = await dashboardService.GetAdminStudentsAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("AdminInstructors")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetAdminInstructors()
        {
            var result = await dashboardService.GetAdminInstructorsAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("RecentSessions")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetRecentSessions()
        {
            var result = await dashboardService.GetRecentSessionsAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("RecentAssignments")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetRecentAssignments()
        {
            var result = await dashboardService.GetRecentAssignmentsAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("TopRatedCourses")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetTopRatedCourses()
        {
            var result = await dashboardService.GetTopRatedCoursesAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("AdminUserDetails/{userId}")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetAdminUserDetails(string userId)
        {
            var result = await dashboardService.GetAdminUserDetailsAsync(userId);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("RevenueTrend")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetRevenueTrend([FromQuery] int days = 30)
        {
            var result = await dashboardService.GetRevenueTrendAsync(days);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("EnrollmentsTrend")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetEnrollmentsTrend([FromQuery] int days = 30)
        {
            var result = await dashboardService.GetEnrollmentsTrendAsync(days);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("UsersByRole")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetUsersByRole()
        {
            var result = await dashboardService.GetUsersByRoleAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("TopCoursesChart")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetTopCoursesChart()
        {
            var result = await dashboardService.GetTopCoursesChartAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        private async Task<IActionResult> GetDashboardOverview()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new { success = false, message = "You must be logged in" });
            }

            if (!User.HasRole(AppRoles.Admin) &&
                !User.HasRole(AppRoles.OrganizationAdmin) &&
                !User.HasRole(AppRoles.Instructor))
            {
                return Forbid();
            }

            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await dashboardService.GetOrganizationStatsAsync(
                    currentUserId,
                    User.HasRole(AppRoles.Admin),
                    User.HasRole(AppRoles.OrganizationAdmin),
                    User.HasRole(AppRoles.Instructor));

                if (!result.success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Unexpected error while retrieving organization stats"
                });
            }
        }
    }
}
