namespace Application.Configuration
{
    public sealed class PaymobOptions
    {
        public const string SectionName = "Paymob";

        public string BaseUrl { get; set; } = "https://accept.paymob.com/api/";
        public string ApiKey { get; set; } = string.Empty;
        public string HmacSecret { get; set; } = string.Empty;
        public int IFrameId { get; set; }
        public int CardIntegrationId { get; set; }
        public int BankTransferIntegrationId { get; set; }
        public int InstallmentsIntegrationId { get; set; }
        public int WalletIntegrationId { get; set; }
        public int PaymentKeyExpirationSeconds { get; set; } = 3600;
        public string Currency { get; set; } = "EGP";
    }
}
