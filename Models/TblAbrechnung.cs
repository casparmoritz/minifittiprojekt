// ============================================================
// TblAbrechnung.cs – Model für die Oracle-Tabelle TBL_ABRECHNUNG
// Monatliche Abrechnung pro Kunde mit Betrag und Ermäßigung
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace csharp_webapi.Models
{
    [Table("TBL_ABRECHNUNG")]
    public class TblAbrechnung
    {
        // AbrechnungsNr wird von Oracle automatisch generiert (IDENTITY)
        [Key]
        [Column("ABRECHNUNGSNR")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Abrechnungsnr { get; set; }

        // Fremdschlüssel → TBL_KUNDEN.KUNDENNR (nullable laut SQL)
        [Column("KUNDENNR")]
        public int? Kundennr { get; set; }

        // Fremdschlüssel → TBL_ABO.ABONR (NOT NULL laut SQL)
        [Column("ABONR")]
        [Required]
        public int Abonr { get; set; }

        // Fremdschlüssel → TBL_ERMAESSIGTE.ERMID (nullable)
        [Column("ERMID")]
        public int? Ermid { get; set; }

        // Fremdschlüssel → TBL_KUNDENABO.KUNDENABONR (nullable, ALTER TABLE nachträglich hinzugefügt)
        [Column("KUNDENABONR")]
        public int? Kundenabonr { get; set; }

        // Rechnungsbetrag nach Ermäßigung (NUMBER(3,2) in Oracle)
        [Column("RECHNUNGSBETRAG", TypeName = "decimal(18,2)")]
        public decimal Rechnungsbetrag { get; set; }

        // Abrechnungsmonat (DATE in Oracle, NOT NULL – wird als erster Tag des Monats gespeichert)
        // Beispiel: 01.05.2026 = Abrechnung für Mai 2026
        [Column("ABRECHNUNGSMONAT")]
        [Required]
        public DateTime Abrechnungsmonat { get; set; }

        // ── Navigation Properties (werden beim JSON-Serialisieren NICHT ignoriert,
        //    damit der GET-Endpunkt Kundendaten direkt mitliefert)
        // Wichtig: JsonIgnore fehlt absichtlich hier, damit die View-Abfrage funktioniert.
        // Der AppDbContext konfiguriert ReferenceHandler.IgnoreCycles, um Endlosschleifen zu verhindern.

        [ForeignKey(nameof(Kundennr))]
        public TblKunde? Kunde { get; set; }

        [ForeignKey(nameof(Abonr))]
        public TblAbo? Abo { get; set; }

        [ForeignKey(nameof(Ermid))]
        public TblErmaessigte? Ermaessigte { get; set; }

        [ForeignKey(nameof(Kundenabonr))]
        public TblKundenAbo? KundenAbo { get; set; }
    }
}
