// ============================================================
// TblKundenAbo.cs – Model für die Oracle-Tabelle TBL_KUNDENABO
// Verknüpft Kunden mit Abos inkl. Laufzeit und Status
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace csharp_webapi.Models
{
    [Table("TBL_KUNDENABO")]
    public class TblKundenAbo
    {
        // KundenAboNr wird von Oracle automatisch generiert (IDENTITY)
        [Key]
        [Column("KUNDENABONR")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Kundenabonr { get; set; }

        // Fremdschlüssel → TBL_KUNDEN.KUNDENNR
        [Column("KUNDENNR")]
        [Required]
        public int Kundennr { get; set; }

        // Fremdschlüssel → TBL_ABO.ABONR
        [Column("ABONR")]
        [Required]
        public int Abonr { get; set; }

        // Startdatum des Abo-Vertrags
        [Column("STARTDATUM")]
        [Required]
        public DateTime Startdatum { get; set; }

        // Enddatum des Abo-Vertrags
        [Column("ENDDATUM")]
        [Required]
        public DateTime Enddatum { get; set; }

        // Status des Vertrags: 'AKTIV', 'ABGELAUFEN' oder 'GEKUENDIGT'
        // DEFAULT 'AKTIV' in Oracle
        [Column("STATUS")]
        [StringLength(10)]
        public string Status { get; set; } = "AKTIV";

        // Navigation: Kunde zu diesem Vertrag
        [ForeignKey(nameof(Kundennr))]
        public TblKunde? Kunde { get; set; }

        // Navigation: Abo-Typ zu diesem Vertrag
        [ForeignKey(nameof(Abonr))]
        public TblAbo? Abo { get; set; }

        // Navigation: Abrechnungen die zu diesem KundenAbo gehören
        [JsonIgnore]
        public ICollection<TblAbrechnung> Abrechnungen { get; set; } = new List<TblAbrechnung>();
    }
}
