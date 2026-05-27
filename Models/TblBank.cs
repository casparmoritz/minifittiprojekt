using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace csharp_webapi.Models
{
    [Table("TBL_BANK")]
    public class TblBank
    {
        [Key]
        [Column("BIC")]
        [StringLength(11)]
        public required string Bic { get; set; }

        [Column("BANK")]
        [Required]
        public required string BankName { get; set; }

        [JsonIgnore]
        public ICollection<TblKonto> Konten { get; set; } = new List<TblKonto>();
    }
}
