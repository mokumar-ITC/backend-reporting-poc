using BIEmbedSystem.Services.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.Interfaces
{
    public interface ITranslationService
    {
        Task<TranslateSidebarResponse> TranslateAsync(
            List<string> texts,
            string targetLanguage
        );
    }
}
