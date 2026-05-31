using Application.DTOs.Payment;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services.Implementitions
{
    public class PaymentService(
        IGeneric<Payment> paymentsManagement,
        IGeneric<Course> coursesManagement,
        IUserManagment userManagement) : IPaymentService
    {
        public async Task<ServiceResponse> GetAdminSummaryAsync()
        {
            var payments = (await paymentsManagement.GetAllAsync()).ToList();
            var activeCourseIds = (await coursesManagement.GetAllAsync())
                .Where(c => !c.IsDeleted)
                .Select(c => c.Id)
                .ToHashSet();

            payments = payments.Where(p => activeCourseIds.Contains(p.CourseId)).ToList();

            var summary = new AdminPaymentSummaryDto
            {
                TotalPayments = payments.Count,
                PaidPayments = payments.Count(p => IsStatus(p, "Paid")),
                PendingPayments = payments.Count(p => IsStatus(p, "Pending")),
                FailedPayments = payments.Count(p => IsStatus(p, "Failed")),
                TotalRevenue = payments.Where(p => IsStatus(p, "Paid")).Sum(p => p.TotalPrice)
            };

            return new ServiceResponse(true, "Payment summary retrieved successfully", summary);
        }

        public async Task<ServiceResponse> GetOrganizationSummaryAsync(string organizationAdminId)
        {
            if (string.IsNullOrWhiteSpace(organizationAdminId))
            {
                return new ServiceResponse(false, "Organization user id is missing");
            }

            var activeCourseIds = (await coursesManagement.GetAllAsync())
                .Where(c => !c.IsDeleted && c.OrgId == organizationAdminId)
                .Select(c => c.Id)
                .ToHashSet();

            var payments = (await paymentsManagement.GetAllAsync())
                .Where(p => activeCourseIds.Contains(p.CourseId))
                .ToList();

            var summary = new AdminPaymentSummaryDto
            {
                TotalPayments = payments.Count,
                PaidPayments = payments.Count(p => IsStatus(p, "Paid")),
                PendingPayments = payments.Count(p => IsStatus(p, "Pending")),
                FailedPayments = payments.Count(p => IsStatus(p, "Failed")),
                TotalRevenue = payments.Where(p => IsStatus(p, "Paid")).Sum(p => p.TotalPrice)
            };

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

            var courses = (await coursesManagement.GetAllAsync())
                .Where(c => !c.IsDeleted)
                .ToDictionary(c => c.Id, c => c);

            var paymentsQuery = (await paymentsManagement.GetAllAsync())
                .Where(p => courses.ContainsKey(p.CourseId))
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                paymentsQuery = paymentsQuery.Where(p => IsStatus(p, status));
            }

            if (fromDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.SubmittingDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.SubmittingDate.Date <= toDate.Value.Date);
            }

            var allItems = new List<AdminPaymentTransactionDto>();
            foreach (var payment in paymentsQuery.OrderByDescending(p => p.SubmittingDate))
            {
                courses.TryGetValue(payment.CourseId, out var course);
                var student = await userManagement.GetUserById(payment.StudentId);

                allItems.Add(new AdminPaymentTransactionDto
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

            if (!string.IsNullOrWhiteSpace(search))
            {
                allItems = allItems.Where(item =>
                    ContainsSearch(item.CourseName, search) ||
                    ContainsSearch(item.StudentName, search) ||
                    ContainsSearch(item.StudentEmail, search) ||
                    ContainsSearch(item.MerchantOrderId, search) ||
                    ContainsSearch(item.SpecialReference, search))
                    .ToList();
            }

            var totalCount = allItems.Count;
            var items = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var response = new PaginatedResponse<AdminPaymentTransactionDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = items
            };

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

            var courses = (await coursesManagement.GetAllAsync())
                .Where(c => !c.IsDeleted && c.OrgId == organizationAdminId)
                .ToDictionary(c => c.Id, c => c);

            var paymentsQuery = (await paymentsManagement.GetAllAsync())
                .Where(p => courses.ContainsKey(p.CourseId))
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                paymentsQuery = paymentsQuery.Where(p => IsStatus(p, status));
            }

            if (fromDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.SubmittingDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.SubmittingDate.Date <= toDate.Value.Date);
            }

            var allItems = new List<AdminPaymentTransactionDto>();
            foreach (var payment in paymentsQuery.OrderByDescending(p => p.SubmittingDate))
            {
                courses.TryGetValue(payment.CourseId, out var course);
                var student = await userManagement.GetUserById(payment.StudentId);

                allItems.Add(new AdminPaymentTransactionDto
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

            if (!string.IsNullOrWhiteSpace(search))
            {
                allItems = allItems.Where(item =>
                    ContainsSearch(item.CourseName, search) ||
                    ContainsSearch(item.StudentName, search) ||
                    ContainsSearch(item.StudentEmail, search) ||
                    ContainsSearch(item.MerchantOrderId, search) ||
                    ContainsSearch(item.SpecialReference, search))
                    .ToList();
            }

            var totalCount = allItems.Count;
            var response = new PaginatedResponse<AdminPaymentTransactionDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };

            return new ServiceResponse(true, "Organization payment transactions retrieved successfully", response);
        }

        private static bool IsStatus(Payment payment, string status)
        {
            return string.Equals(payment.PaymentStatus, status, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsSearch(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }
    }
}
