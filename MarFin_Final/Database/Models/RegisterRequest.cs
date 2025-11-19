namespace MarFin_Final.Models
{
    public class RegisterRequest
    {
        public int RoleId { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? Phone { get; set; }
        public string? Department { get; set; }
    }
}