using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    [Table("PBI_AzureAd_Info")]
    public class PBIAzureAdInfo: BaseClass
    {
        [Key]
        public int Id { get; set; }

        public string? TenantId { get; set; }

        public string? ClientId { get; set; }

        public string? ClientSecret { get; set; }

        public string? ScopeBase { get; set; }

        public string? AuthenticationMode { get; set; }
    }
}
