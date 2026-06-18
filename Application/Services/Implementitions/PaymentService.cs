using Application.DTOs.Payment;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementitions
{
    public class PaymentService(
        IGeneric<Payment> paymentsManagement,
        IGeneric<Course> coursesManagement,
        IUserManagment userManagement,
        ILogger<PaymentService> logger) : IPaymentService
    {
        public async Task<ServiceResponse> GetAdminSummaryAsync()
        {
            var activeCourseIds = coursesManagement.Query()
                .Where(course => !course.IsDeleted)
                .Select(course => course.Id);
            var summary = await BuildSummaryAsync(
                paymentsManagement.Query().Where(payment => activeCourseIds.Contains(payment.CourseId)));

            return new ServiceResponse(true, "Payment summary retrieved successfully", summary);
        }

        public async Task<ServiceResponse> GetOrganizationSummaryAsync(string organizationAdminId)
        {
            if (string.IsNullOrWhiteSpace(organizationAdminId))
            {
                return new ServiceResponse(false, "Organization user id is missing");
            }

            var organizationId = await ResolveUserOrganizationIdAsync(organizationAdminId);
            if (!organizationId.HasValue)
                return new ServiceResponse(false, "User is not assigned to an organization");

            var activeCourseIds = coursesManagement.Query()
                .Where(course => !course.IsDeleted && course.OrganizationId == organizationId.Value)
                .Select(course => course.Id);
            var summary = await BuildSummaryAsync(
                paymentsManagement.Query().Where(payment => activeCourseIds.Contains(payment.CourseId)));

            return new ServiceResponse(true, "Organization payment summary retrieved successfully", summary);
        }

        public async Task<ServiceResponse> GetAdminTransactionsAsync(
            int page,
            int pageSize,
            string? status,
            string? search,
            DateTime? fromDate,
            DateTime? toDate)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

            var response = await BuildTransactionsAsync(
                coursesManagement.Query().Where(course => !course.IsDeleted),
                page,
                pageSize,
                status,
                search,
                fromDate,
                toDate);
            return new ServiceResponse(true, "Payment transactions retrieved successfully", response);
        }

        public async Task<ServiceResponse> GetOrganizationTransactionsAsync(
            string organizationAdminId,
            int page,
            int pageSize,
            string? status,
            string? search,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (string.IsNullOrWhiteSpace(organizationAdminId))
            {
                return new ServiceResponse(false, "Organization user id is missing");
            }

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

            var organizationId = await ResolveUserOrganizationIdAsync(organizationAdminId);
            if (!organizationId.HasValue)
                return new ServiceResponse(false, "User is not assigned to an organization");

            var response = await BuildTransactionsAsync(
                coursesManagement.Query().Where(course =>
                    !course.IsDeleted &&
                    course.OrganizationId == organizationId.Value),
                page,
                pageSize,
                status,
                search,
                fromDate,
                toDate);
            return new ServiceResponse(true, "Organization payment transactions retrieved successfully", response);
        }

        private async Task<AdminPaymentSummaryDto> BuildSummaryAsync(IQueryable<Payment> query)
        {
            return await query
                .GroupBy(_ => 1)
                .Select(group => new AdminPaymentSummaryDto
                {
                    TotalPayments = group.Count(),
                    PaidPayments = group.Count(payment => payment.PaymentStatus == "Paid"),
                    PendingPayments = group.Count(payment => payment.PaymentStatus == "Pending"),
                    FailedPayments = group.Count(payment => payment.PaymentStatus == "Failed"),
                    TotalRevenue = group
                        .Where(payment => payment.PaymentStatus == "Paid")
                        .Sum(payment => (double?)payment.TotalPrice) ?? 0
                })
                .FirstOrDefaultAsync() ?? new AdminPaymentSummaryDto();
        }

        private async Task<PaginatedResponse<AdminPaymentTransactionDto>> BuildTransactionsAsync(
            IQueryable<Course> coursesQuery,
            int page,
            int pageSize,
            string? status,
            string? search,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query =
                from payment in paymentsManagement.Query()
                join course in coursesQuery on payment.CourseId equals course.Id
                join student in userManagement.QueryUsers() on payment.StudentId equals student.Id
                select new AdminPaymentTransactionDto
                {
                    CourseId = payment.CourseId,
                    CourseName = course.Name,
                    StudentId = payment.StudentId,
                    StudentName = student.FullName ?? string.Empty,
                    StudentEmail = student.Email ?? string.Empty,
                    SubmittingDate = payment.SubmittingDate,
                    TotalPrice = payment.TotalPrice,
                    PaymentMethod = payment.PaymentMethod,
                    PaymentStatus = payment.PaymentStatus,
                    PaymentProvider = payment.PaymentProvider,
                    MerchantOrderId = payment.MerchantOrderId,
                    SpecialReference = payment.SpecialReference
                };

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(item => item.PaymentStatus == status);
            if (fromDate.HasValue)
                query = query.Where(item => item.SubmittingDate >= fromDate.Value.Date);
            if (toDate.HasValue)
            {
                var exclusiveEnd = toDate.Value.Date.AddDays(1);
                query = query.Where(item => item.SubmittingDate < exclusiveEnd);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(item =>
                    item.CourseName.Contains(term) ||
                    item.StudentName.Contains(term) ||
                    item.StudentEmail.Contains(term) ||
                    (item.MerchantOrderId != null && item.MerchantOrderId.Contains(term)) ||
                    (item.SpecialReference != null && item.SpecialReference.Contains(term)));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(item => item.SubmittingDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            logger.LogInformation(
                "Payment transaction query returned {ItemCount}/{TotalCount} rows for page {Page}",
                items.Count,
                totalCount,
                page);

            return new PaginatedResponse<AdminPaymentTransactionDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = items
            };
        }

        private async Task<Guid?> ResolveUserOrganizationIdAsync(string userId)
        {
            var user = await userManagement.GetUserById(userId);
            return user?.OrganizationId;
        }
    }
}
