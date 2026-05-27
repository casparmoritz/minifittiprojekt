// ============================================================
// AbrechnungenController.cs – REST-API für Abrechnungen
// Route: /api/abrechnungen
// Erweitert: Filterung nach Monat (?monat=YYYY-MM) und Jahr (?jahr=YYYY)
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharp_webapi.Data;
using csharp_webapi.Models;

namespace csharp_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AbrechnungenController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AbrechnungenController(AppDbContext context)
        {
            _context = context;
        }

        // ── GET /api/abrechnungen ────────────────────────────────────────────
        // Gibt Abrechnungen zurück; optional gefiltert:
        //   ?monat=2026-05   → Monatsübersicht (alle Abrechnungen dieses Monats)
        //   ?jahr=2026       → Jahresübersicht (12 Monatsobjekte mit Summen)
        // Ohne Parameter: alle Abrechnungen (inkl. Kunde, Abo, Ermäßigung)
        [HttpGet]
        public async Task<IActionResult> GetAbrechnungen(
            [FromQuery] string? monat,   // Format: YYYY-MM
            [FromQuery] int?    jahr)    // Format: YYYY
        {
            // ── Filter: Monatsübersicht ──────────────────────────────────────
            if (!string.IsNullOrEmpty(monat))
            {
                // Monat parsen (z.B. "2026-05" → Jahr=2026, Monat=5)
                if (!DateTime.TryParseExact(monat + "-01",
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var monatStart))
                {
                    return BadRequest("Ungültiges Monatsformat. Erwartet: YYYY-MM");
                }

                var monatEnde = monatStart.AddMonths(1);

                // Abrechnungen des gewählten Monats laden
                var abrechnungen = await _context.Abrechnungen
                    .Include(a => a.Kunde)
                    .Include(a => a.Abo)
                    .Include(a => a.Ermaessigte)
                    .Where(a => a.Abrechnungsmonat >= monatStart &&
                                a.Abrechnungsmonat <  monatEnde)
                    .ToListAsync();

                // In das Format umwandeln das das Vue-Frontend erwartet
                var result = abrechnungen.Select(a => new
                {
                    abrechnungsNr  = a.Abrechnungsnr,
                    kundenNr       = a.Kundennr,
                    kundenName     = $"{a.Kunde?.Vorname} {a.Kunde?.Nachname}".Trim(),
                    aboBezeichnung = $"Abo #{a.Abonr}",
                    grundpreis     = a.Abo?.Grundpreis ?? 0,
                    ermaessigung   = (a.Ermaessigte?.Ermaessigungssatz ?? 0) * 100, // in % für das Frontend
                    endbetrag      = a.Rechnungsbetrag,
                    sonderangebot  = (string?)null // Erweiterbar für zukünftige Sonderangebote
                });

                return Ok(result);
            }

            // ── Filter: Jahresübersicht ──────────────────────────────────────
            if (jahr.HasValue)
            {
                var jahresStart = new DateTime(jahr.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var jahresEnde  = new DateTime(jahr.Value + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                // Alle Abrechnungen des Jahres laden
                var alleAbrechnungen = await _context.Abrechnungen
                    .Include(a => a.Abo)
                    .Include(a => a.Ermaessigte)
                    .Where(a => a.Abrechnungsmonat >= jahresStart &&
                                a.Abrechnungsmonat <  jahresEnde)
                    .ToListAsync();

                // Monatsnamen auf Deutsch
                var monateNamen = new[]
                {
                    "Januar","Februar","März","April","Mai","Juni",
                    "Juli","August","September","Oktober","November","Dezember"
                };
                var monateKurz = new[]
                {
                    "Jan","Feb","Mär","Apr","Mai","Jun",
                    "Jul","Aug","Sep","Okt","Nov","Dez"
                };

                // 12 Monatsobjekte aufbauen
                var jahresDaten = Enumerable.Range(1, 12).Select(m =>
                {
                    // Abrechnungen dieses Monats filtern
                    var monatsDaten = alleAbrechnungen
                        .Where(a => a.Abrechnungsmonat.Month == m)
                        .ToList();

                    var summe   = monatsDaten.Sum(a => a.Abo?.Grundpreis ?? 0);
                    var rabatte = monatsDaten.Sum(a =>
                        Math.Max(0, (a.Abo?.Grundpreis ?? 0) - a.Rechnungsbetrag));
                    var netto   = monatsDaten.Sum(a => a.Rechnungsbetrag);

                    return new
                    {
                        name     = monateNamen[m - 1],
                        kurzname = monateKurz[m - 1],
                        anzahl   = monatsDaten.Count,
                        summe    = summe,
                        rabatte  = rabatte,
                        netto    = netto
                    };
                });

                return Ok(jahresDaten);
            }

            // ── Keine Filter: Alle Abrechnungen zurückgeben ──────────────────
            var alle = await _context.Abrechnungen
                .Include(a => a.Kunde)
                .Include(a => a.Abo)
                .Include(a => a.Ermaessigte)
                .Include(a => a.KundenAbo)
                .ToListAsync();

            return Ok(alle);
        }

        // ── GET /api/abrechnungen/{id} ───────────────────────────────────────
        // Gibt eine einzelne Abrechnung zurück
        [HttpGet("{id}")]
        public async Task<ActionResult<TblAbrechnung>> GetAbrechnung(int id)
        {
            var abrechnung = await _context.Abrechnungen
                .Include(a => a.Kunde)
                .Include(a => a.Abo)
                .Include(a => a.Ermaessigte)
                .Include(a => a.KundenAbo)
                .FirstOrDefaultAsync(a => a.Abrechnungsnr == id);

            if (abrechnung == null)
                return NotFound($"Abrechnung mit AbrechnungsNr {id} nicht gefunden.");

            return abrechnung;
        }

        // ── POST /api/abrechnungen ───────────────────────────────────────────
        // Legt eine neue Abrechnung an
        // Body-Beispiel:
        // {
        //   "kundennr": 1, "abonr": 1, "ermid": 1,
        //   "rechnungsbetrag": 39.99,
        //   "abrechnungsmonat": "2026-05-01T00:00:00Z"
        // }
        [HttpPost]
        public async Task<ActionResult<TblAbrechnung>> PostAbrechnung(TblAbrechnung abrechnung)
        {
            // Kunde validieren (falls angegeben)
            if (abrechnung.Kundennr.HasValue &&
                !await _context.Kunden.AnyAsync(k => k.Kundennr == abrechnung.Kundennr))
            {
                return BadRequest($"Kundennummer {abrechnung.Kundennr} existiert nicht.");
            }

            // Abo validieren (Pflichtfeld)
            if (!await _context.Abos.AnyAsync(a => a.Abonr == abrechnung.Abonr))
                return BadRequest($"AboNr {abrechnung.Abonr} existiert nicht.");

            // Ermäßigung validieren (falls angegeben)
            if (abrechnung.Ermid.HasValue &&
                !await _context.Ermaessigte.AnyAsync(e => e.Ermid == abrechnung.Ermid))
            {
                return BadRequest($"ErmID {abrechnung.Ermid} existiert nicht.");
            }

            // Navigation-Properties leeren um ungewollte DB-Inserts zu verhindern
            abrechnung.Kunde      = null;
            abrechnung.Abo        = null;
            abrechnung.Ermaessigte = null;
            abrechnung.KundenAbo  = null;

            _context.Abrechnungen.Add(abrechnung);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAbrechnung), new { id = abrechnung.Abrechnungsnr }, abrechnung);
        }

        // ── PUT /api/abrechnungen/{id} ───────────────────────────────────────
        // Aktualisiert eine bestehende Abrechnung
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAbrechnung(int id, TblAbrechnung abrechnung)
        {
            if (id != abrechnung.Abrechnungsnr)
                return BadRequest("AbrechnungsNr in der URL stimmt nicht mit dem Body überein.");

            // Validierungen (gleich wie beim POST)
            if (abrechnung.Kundennr.HasValue &&
                !await _context.Kunden.AnyAsync(k => k.Kundennr == abrechnung.Kundennr))
            {
                return BadRequest($"Kundennummer {abrechnung.Kundennr} existiert nicht.");
            }

            if (!await _context.Abos.AnyAsync(a => a.Abonr == abrechnung.Abonr))
                return BadRequest($"AboNr {abrechnung.Abonr} existiert nicht.");

            if (abrechnung.Ermid.HasValue &&
                !await _context.Ermaessigte.AnyAsync(e => e.Ermid == abrechnung.Ermid))
            {
                return BadRequest($"ErmID {abrechnung.Ermid} existiert nicht.");
            }

            // Navigation-Properties leeren
            abrechnung.Kunde      = null;
            abrechnung.Abo        = null;
            abrechnung.Ermaessigte = null;
            abrechnung.KundenAbo  = null;

            _context.Entry(abrechnung).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Abrechnungen.AnyAsync(a => a.Abrechnungsnr == id))
                    return NotFound($"Abrechnung mit AbrechnungsNr {id} nicht gefunden.");
                throw;
            }

            return NoContent();
        }

        // ── DELETE /api/abrechnungen/{id} ────────────────────────────────────
        // Löscht eine Abrechnung
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAbrechnung(int id)
        {
            var abrechnung = await _context.Abrechnungen.FindAsync(id);
            if (abrechnung == null)
                return NotFound($"Abrechnung mit AbrechnungsNr {id} nicht gefunden.");

            _context.Abrechnungen.Remove(abrechnung);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
