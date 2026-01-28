using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class TranslationRequest
    {
        public string Text { get; set; }
        public string ToLanguage { get; set; }
    }
}
