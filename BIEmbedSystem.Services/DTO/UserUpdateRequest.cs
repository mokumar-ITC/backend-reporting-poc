namespace BIEmbedSystem.Services.DTO.Requests
{
    public class UserUpdateRequest
    {
        public int OrganizationId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public int Role { get; set; } = 0;
        public bool IsActive { get; set; }
        public string? Password { get; set; } // optional
    }
}
