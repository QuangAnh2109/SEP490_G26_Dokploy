namespace Backend.DTOs
{
    public class GoogleCompleteProfileRequest
    {
        public string IdToken { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? StudentId { get; set; }
    }
}

