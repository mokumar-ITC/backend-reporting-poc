namespace BIEmbedSystem.Services.DTO.Requests
{
    public class UserCreateRequest
    {
        public int OrganizationId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public int Role { get; set; } = 0;
        public string Password { get; set; } = "";
    }
}
