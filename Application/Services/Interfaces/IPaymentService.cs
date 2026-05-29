using Application.DTOs.Responses;

namespace Application.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ServiceResponse> GetAdminSummaryAsync();
        Task<ServiceResponse> GetAdminTransactionsAsync(int page, int pageSize);
    }
}
