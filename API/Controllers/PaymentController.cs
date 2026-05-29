using API.Authorization;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PaymentController(IPaymentService paymentService) : ControllerBase
    {
        [HttpGet("AdminSummary")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetAdminSummary()
        {
            var result = await paymentService.GetAdminSummaryAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("AdminTransactions")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetAdminTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await paymentService.GetAdminTransactionsAsync(page, pageSize);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
