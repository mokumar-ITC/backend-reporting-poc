using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    [Table("PasswordResetOtps")]
    public class PasswordResetOtp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Otp { get; set; } = "";
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; set; } = false;
    }

}
