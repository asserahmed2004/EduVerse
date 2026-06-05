using API.Authorization;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PaymentController(IPaymentService paymentService) : ControllerBase
    {
        [HttpGet("AdminSummary")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetAdminSummary()
        {
            var result = await paymentService.GetAdminSummaryAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("AdminTransactions")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetAdminTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await paymentService.GetAdminTransactionsAsync(page, pageSize, status, search, fromDate, toDate);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("OrganizationSummary")]
        [Authorize(Roles = AppRoles.OrganizationAdminAccess)]
        public async Task<IActionResult> GetOrganizationSummary()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await paymentService.GetOrganizationSummaryAsync(userId);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("OrganizationTransactions")]
        [Authorize(Roles = AppRoles.OrganizationAdminAccess)]
        public async Task<IActionResult> GetOrganizationTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await paymentService.GetOrganizationTransactionsAsync(userId, page, pageSize, status, search, fromDate, toDate);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
