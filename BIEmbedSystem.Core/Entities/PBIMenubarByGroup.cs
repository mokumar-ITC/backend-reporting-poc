using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    [Table("PBI_Menubar_By_Group")]
    public class PBIMenubarByGroup :BaseClass
    {
        public int Id { get; set; }
     
        public string? MenuName { get; set; }

        public string? GroupName { get; set; }
    }
}
