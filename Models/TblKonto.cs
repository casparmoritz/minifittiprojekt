using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace csharp_webapi.Models
{
    [Table("TBL_KONTO")]
    public class TblKonto
    {
        [Key]
        [Column("IBAN")]
        [StringLength(22, MinimumLength = 22, ErrorMessage = "Die IBAN muss genau 22 Zeichen lang sein.")]
        public required string Iban { get; set; }

        [Column("BIC")]
        [Required]
        [StringLength(11)]
        public required string Bic { get; set; }

        [ForeignKey(nameof(Bic))]
        public TblBank? Bank { get; set; }

        [JsonIgnore]
        public ICollection<TblKunde> Kunden { get; set; } = new List<TblKunde>();
    }
}
