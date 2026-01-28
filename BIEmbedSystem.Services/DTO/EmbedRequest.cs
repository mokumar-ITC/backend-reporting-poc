using Microsoft.PowerBI.Api.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class EmbedRequest
    {
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; } = null;
    }
}
