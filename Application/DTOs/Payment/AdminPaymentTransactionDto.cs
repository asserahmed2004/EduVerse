namespace Application.DTOs.Payment
{
    public class AdminPaymentTransactionDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public DateTime SubmittingDate { get; set; }
        public double TotalPrice { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentProvider { get; set; } = string.Empty;
        public string? MerchantOrderId { get; set; }
        public string? SpecialReference { get; set; }
    }
}
