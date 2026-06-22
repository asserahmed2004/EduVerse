namespace Application.Exceptions
{
    public sealed class PaymobException : Exception
    {
        public PaymobException(
            string message,
            string clientMessage,
            int statusCode,
            int? providerStatusCode = null,
            string? providerResponse = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            ClientMessage = clientMessage;
            StatusCode = statusCode;
            ProviderStatusCode = providerStatusCode;
            ProviderResponse = providerResponse;
        }

        public string ClientMessage { get; }
        public int StatusCode { get; }
        public int? ProviderStatusCode { get; }
        public string? ProviderResponse { get; }
    }
}
