using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class FabricOpenAIRequest
    {
        public string Model { get; set; } = "fabric-data-agent";
        public List<Message> Messages { get; set; } = new();
    }
}
