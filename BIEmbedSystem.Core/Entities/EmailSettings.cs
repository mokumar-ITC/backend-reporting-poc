using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    public class EmailSettings
    {
        public string SenderName { get; set; } = "Reporting Hub";
        public string SenderEmail { get; set; } = "reporting_poc@itconvergence.com";
        public bool UseGmailApi { get; set; } = true;
        public string GmailClientId { get; set; } = "";
        public string GmailClientSecret { get; set; } = "";
        public string GmailRefreshToken { get; set; } = "";
    }
}

