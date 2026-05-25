namespace Application.DTOs.Responses
{
    public record ServiceResponse
        (
        bool success = false,
        string message = null,
        object data = null
        );
}
