using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class RoleBulkUploadRequest
    {
        public IFormFile File { get; set; } = null!;
    }
}
