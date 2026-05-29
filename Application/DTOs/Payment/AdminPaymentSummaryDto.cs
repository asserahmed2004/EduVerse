namespace Application.DTOs.Payment
{
    public class AdminPaymentSummaryDto
    {
        public int TotalPayments { get; set; }
        public int PaidPayments { get; set; }
        public int PendingPayments { get; set; }
        public int FailedPayments { get; set; }
        public double TotalRevenue { get; set; }
    }
}
