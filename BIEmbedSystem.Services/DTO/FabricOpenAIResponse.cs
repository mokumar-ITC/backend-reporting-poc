using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class FabricOpenAIResponse
    {
        public List<FabricChoice> Choices { get; set; }
    }

    public class FabricChoice
    {
        public Message Message { get; set; }
    }
}
