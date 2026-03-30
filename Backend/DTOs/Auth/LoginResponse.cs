namespace Backend.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool NeedsRegistration { get; set; }
        public bool NeedsProfileCompletion { get; set; }
        public string Email { get; set; } = string.Empty;
        public List<string>? MissingFields { get; set; }
    }
}
