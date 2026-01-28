using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class TranslateSidebarRequest
    {
        public List<string> Texts { get; set; } = new();
        public string TargetLanguage { get; set; } = "te"; // e.g. te, hi, fr
    }
}
