using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace csharp_webapi.Models
{
    [Table("TBL_KUNDEN")]
    public class TblKunde
    {
        [Key]
        [Column("KUNDENNR")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Kundennr { get; set; }

        [Column("VORNAME")]
        public string? Vorname { get; set; }

        [Column("NACHNAME")]
        public string? Nachname { get; set; }

        [Column("IBAN")]
        [StringLength(22, MinimumLength = 22, ErrorMessage = "Die IBAN muss genau 22 Zeichen lang sein.")]
        public string? Iban { get; set; }

        [ForeignKey(nameof(Iban))]
        public TblKonto? Konto { get; set; }

        [NotMapped]
        public int? Abonr { get; set; }

        [NotMapped]
        public int? Ermid { get; set; }

        [JsonIgnore]
        public ICollection<TblAbrechnung> Abrechnungen { get; set; } = new List<TblAbrechnung>();
    }
}
