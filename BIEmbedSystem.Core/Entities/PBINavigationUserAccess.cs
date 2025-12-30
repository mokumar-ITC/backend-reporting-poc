using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIEmbedSystem.Core.Entities
{
    [Table("PBI_User_Nagivation")]
    public class PBINavigationUserAccess : BaseClass
    {
        public int Id { get; set; }                 // PK

        public string UserId { get; set; }          // nvarchar(500) NOT NULL
        public string UserEmail { get; set; }       // nvarchar(100) NOT NULL

        public int NagivationId { get; set; }       // int NOT NULL

        // Existing permissions
        public bool ShowDatasetPane { get; set; }   // bit NOT NULL
        public bool ShowEdit { get; set; }          // bit NOT NULL
        public bool ShowBookmark { get; set; }      // bit NOT NULL

        // ✅ NEW permissions
        public bool ShareReport { get; set; }       // bit NOT NULL
        public bool ExportReport { get; set; }      // bit NOT NULL
        public bool ScheduleReport { get; set; }    // bit NOT NULL
        public bool ScheduleSemantic { get; set; }  // bit NOT NULL

        // Meta
        public bool IsActive { get; set; }           // bit NOT NULL
        public int? OrganizationId { get; set; }    // int NULL
    }
}
