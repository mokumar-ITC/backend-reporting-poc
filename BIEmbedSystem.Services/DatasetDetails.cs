using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services
{
    public class DatasetDetails
    {
        public string id { get; set; }
        public string name { get; set; }
        public string targetStorageMode { get; set; }
        public bool isOnPremGatewayRequired { get; set; }
        public string configuredBy { get; set; }
    }
}
