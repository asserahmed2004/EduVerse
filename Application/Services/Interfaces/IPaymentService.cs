using Application.DTOs.Responses;

namespace Application.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ServiceResponse> GetAdminSummaryAsync();
        Task<ServiceResponse> GetAdminTransactionsAsync(
            int page,
            int pageSize,
            string? status,
            string? search,
            DateTime? fromDate,
            DateTime? toDate);
        Task<ServiceResponse> GetOrganizationSummaryAsync(string organizationAdminId);
        Task<ServiceResponse> GetOrganizationTransactionsAsync(
            string organizationAdminId,
            int page,
            int pageSize,
            string? status,
            string? search,
            DateTime? fromDate,
            DateTime? toDate);
    }
}
