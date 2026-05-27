using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace csharp_webapi.Models
{
    [Table("TBL_ERMAESSIGTE")]
    public class TblErmaessigte
    {
        [Key]
        [Column("ERMID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Ermid { get; set; }

        [Column("ERMAESSIGUNGSSATZ", TypeName = "decimal(18, 2)")]
        public decimal Ermaessigungssatz { get; set; }

        [JsonIgnore]
        public ICollection<TblAbrechnung> Abrechnungen { get; set; } = new List<TblAbrechnung>();
    }
}
