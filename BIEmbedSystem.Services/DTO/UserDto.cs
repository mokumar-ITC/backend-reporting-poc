namespace BIEmbedSystem.Services.DTO
{
    public class UserDto
    {
        public int UserId { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public bool IsActive { get; set; }

        public int Role { get; set; }
        public string RoleName { get; set; }

        public int OrganizationId { get; set; }

        public DateTime CreatedOn { get; set; }

        public string OrganizationName { get; set; }
    }
}
