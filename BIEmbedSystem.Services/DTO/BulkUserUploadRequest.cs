using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BIEmbedSystem.Services.DTO
{
    public class BulkUserUploadRequest
    {
        [Required]
        public IFormFile File { get; set; }
    }
}
