using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class UpdateThemeRequest
    {
        public string CompanyName { get; set; }
        public IFormFile Logo { get; set; }
    }

}
