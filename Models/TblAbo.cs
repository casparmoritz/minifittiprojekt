using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace csharp_webapi.Models
{
    [Table("TBL_ABO")]
    public class TblAbo
    {
        [Key]
        [Column("ABONR")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Abonr { get; set; }

        [Column("KUENDIGUNGSSFRIST")]
        public DateTime? Kuendigsfrist { get; set; }

        [Column("KURS")]
        public bool Kurs { get; set; }

        [Column("GETRAENKE")]
        public bool Getraenke { get; set; }

        [Column("GRUNDPREIS", TypeName = "decimal(18, 2)")]
        public decimal Grundpreis { get; set; }

        [Column("LAUFZEIT")]
        public string? Laufzeit { get; set; }

        [JsonIgnore]
        public ICollection<TblAbrechnung> Abrechnungen { get; set; } = new List<TblAbrechnung>();
    }
}
