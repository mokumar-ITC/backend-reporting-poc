using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class TranslationResponse
    {
        public string Original { get; set; }
        public string Translated { get; set; }
        public string TargetLanguage { get; set; }
    }
}
