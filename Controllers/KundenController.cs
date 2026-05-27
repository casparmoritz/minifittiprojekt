// ============================================================
// KundenController.cs – REST-API für die Kundenverwaltung
// Route: /api/kunden
// Unterstützt: GET (alle / einzeln), POST (anlegen), PUT (ändern), DELETE (löschen)
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharp_webapi.Data;
using csharp_webapi.Models;

namespace csharp_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KundenController : ControllerBase
    {
        // Der Datenbankkontext wird per Dependency Injection bereitgestellt
        private readonly AppDbContext _context;

        public KundenController(AppDbContext context)
        {
            _context = context;
        }

        // ── GET /api/kunden ──────────────────────────────────────────────────
        // Gibt alle Kunden zurück, inkl. Konto (IBAN) und Bank (BIC, Bankname),
        // sowie der aktuellen Ermäßigung und des aktiven Abos.
        [HttpGet]
        public async Task<IActionResult> GetKunden()
        {
            var kunden = await _context.Kunden
                .Include(k => k.Konto)
                    .ThenInclude(ko => ko!.Bank)
                .ToListAsync();

            var kundenNrs = kunden.Select(k => k.Kundennr).ToList();

            var kundenAbos = await _context.KundenAbos
                .Include(ka => ka.Abo)
                .Where(ka => kundenNrs.Contains(ka.Kundennr))
                .ToListAsync();

            var abrechnungen = await _context.Abrechnungen
                .Include(a => a.Ermaessigte)
                .Where(a => a.Kundennr.HasValue && kundenNrs.Contains(a.Kundennr.Value))
                .ToListAsync();

            var result = kunden.Select(k =>
            {
                var kAbos = kundenAbos.Where(ka => ka.Kundennr == k.Kundennr).ToList();
                var aktivesAbo = kAbos.FirstOrDefault(ka => ka.Status == "AKTIV") 
                              ?? kAbos.OrderByDescending(ka => ka.Enddatum).FirstOrDefault();

                var kAbrechnungen = abrechnungen.Where(a => a.Kundennr == k.Kundennr).ToList();
                var ermaessigung = kAbrechnungen
                    .OrderByDescending(a => a.Abrechnungsmonat)
                    .Select(a => a.Ermaessigte)
                    .FirstOrDefault();

                return new
                {
                    kundennr = k.Kundennr,
                    vorname = k.Vorname,
                    nachname = k.Nachname,
                    iban = k.Iban,
                    konto = k.Konto,
                    ermaessigung = ermaessigung,
                    aktivesAbo = aktivesAbo != null ? new
                    {
                        kundenabonr = aktivesAbo.Kundenabonr,
                        abonr = aktivesAbo.Abonr,
                        startdatum = aktivesAbo.Startdatum,
                        enddatum = aktivesAbo.Enddatum,
                        status = aktivesAbo.Status,
                        abo = aktivesAbo.Abo != null ? new
                        {
                            abonr = aktivesAbo.Abo.Abonr,
                            grundpreis = aktivesAbo.Abo.Grundpreis,
                            laufzeit = aktivesAbo.Abo.Laufzeit,
                            kurs = aktivesAbo.Abo.Kurs,
                            getraenke = aktivesAbo.Abo.Getraenke
                        } : null
                    } : null
                };
            });

            return Ok(result);
        }

        // ── GET /api/kunden/{id} ─────────────────────────────────────────────
        // Gibt einen einzelnen Kunden anhand der KundenNr zurück
        [HttpGet("{id}")]
        public async Task<IActionResult> GetKunde(int id)
        {
            var k = await _context.Kunden
                .Include(k => k.Konto)
                    .ThenInclude(ko => ko!.Bank)
                .FirstOrDefaultAsync(k => k.Kundennr == id);

            if (k == null)
                return NotFound($"Kunde mit KundenNr {id} nicht gefunden.");

            var kAbos = await _context.KundenAbos
                .Include(ka => ka.Abo)
                .Where(ka => ka.Kundennr == id)
                .ToListAsync();

            var aktivesAbo = kAbos.FirstOrDefault(ka => ka.Status == "AKTIV") 
                          ?? kAbos.OrderByDescending(ka => ka.Enddatum).FirstOrDefault();

            var ermaessigung = await _context.Abrechnungen
                .Include(a => a.Ermaessigte)
                .Where(a => a.Kundennr == id)
                .OrderByDescending(a => a.Abrechnungsmonat)
                .Select(a => a.Ermaessigte)
                .FirstOrDefaultAsync();

            var result = new
            {
                kundennr = k.Kundennr,
                vorname = k.Vorname,
                nachname = k.Nachname,
                iban = k.Iban,
                konto = k.Konto,
                ermaessigung = ermaessigung,
                aktivesAbo = aktivesAbo != null ? new
                {
                    kundenabonr = aktivesAbo.Kundenabonr,
                    abonr = aktivesAbo.Abonr,
                    startdatum = aktivesAbo.Startdatum,
                    enddatum = aktivesAbo.Enddatum,
                    status = aktivesAbo.Status,
                    abo = aktivesAbo.Abo != null ? new
                    {
                        abonr = aktivesAbo.Abo.Abonr,
                        grundpreis = aktivesAbo.Abo.Grundpreis,
                        laufzeit = aktivesAbo.Abo.Laufzeit,
                        kurs = aktivesAbo.Abo.Kurs,
                        getraenke = aktivesAbo.Abo.Getraenke
                    } : null
                } : null
            };

            return Ok(result);
        }

        // ── POST /api/kunden ─────────────────────────────────────────────────
        // Legt einen neuen Kunden an; KundenNr wird von Oracle automatisch vergeben
        // Body-Beispiel: { "vorname": "Max", "nachname": "Muster", "iban": "DE89..." }
        [HttpPost]
        public async Task<ActionResult<TblKunde>> PostKunde(TblKunde kunde)
        {
            // Validierung: IBAN muss in TBL_KONTO existieren (Fremdschlüssel-Prüfung)
            if (!string.IsNullOrEmpty(kunde.Iban) &&
                !await _context.Konten.AnyAsync(k => k.Iban == kunde.Iban))
            {
                return BadRequest($"Die angegebene IBAN '{kunde.Iban}' existiert nicht in TBL_KONTO.");
            }

            // Navigation-Eigenschaft leeren, damit EF nicht versucht das Konto neu einzufügen
            kunde.Konto = null;

            _context.Kunden.Add(kunde);
            await _context.SaveChangesAsync();

            // 201 Created mit Location-Header zurückgeben
            return CreatedAtAction(nameof(GetKunde), new { id = kunde.Kundennr }, kunde);
        }

        // ── PUT /api/kunden/{id} ─────────────────────────────────────────────
        // Aktualisiert einen bestehenden Kunden
        [HttpPut("{id}")]
        public async Task<IActionResult> PutKunde(int id, TblKunde kunde)
        {
            // ID in der URL muss mit dem Body übereinstimmen
            if (id != kunde.Kundennr)
                return BadRequest("KundenNr in der URL stimmt nicht mit dem Body überein.");

            // IBAN-Validierung (nur wenn eine IBAN angegeben wurde)
            if (!string.IsNullOrEmpty(kunde.Iban) &&
                !await _context.Konten.AnyAsync(k => k.Iban == kunde.Iban))
            {
                return BadRequest($"Die angegebene IBAN '{kunde.Iban}' existiert nicht in TBL_KONTO.");
            }

            // Navigation-Eigenschaft leeren um ungewollte Kaskaden zu verhindern
            kunde.Konto = null;
            _context.Entry(kunde).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Prüfe ob der Datensatz inzwischen gelöscht wurde
                if (!await _context.Kunden.AnyAsync(k => k.Kundennr == id))
                    return NotFound($"Kunde mit KundenNr {id} nicht gefunden.");
                throw;
            }

            return NoContent(); // 204 – Erfolgreich, kein Body
        }

        // ── DELETE /api/kunden/{id} ──────────────────────────────────────────
        // Löscht einen Kunden (Abrechnungen werden per Cascade mitgelöscht)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKunde(int id)
        {
            var kunde = await _context.Kunden.FindAsync(id);
            if (kunde == null)
                return NotFound($"Kunde mit KundenNr {id} nicht gefunden.");

            _context.Kunden.Remove(kunde);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 – Erfolgreich gelöscht
        }
    }
}
