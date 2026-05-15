namespace Application.DTOs.Payment
{
    public class GetPayment
    {
        public Guid CourseId { get; set; }
        public string StudentId { get; set; }
        public DateTime SubmittingDate { get; set; }
        public double TotalPrice { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentProvider { get; set; } = string.Empty;
        public string? SpecialReference { get; set; }
        public string? MerchantOrderId { get; set; }
        public string? ProviderIntentionId { get; set; }
        public string? RedirectUrl { get; set; }
        public int? ProviderStatusCode { get; set; }
    }
}
