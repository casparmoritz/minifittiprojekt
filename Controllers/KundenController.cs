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
                    abonr = aktivesAbo?.Abonr,
                    ermid = ermaessigung?.Ermid,
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
                abonr = aktivesAbo?.Abonr,
                ermid = ermaessigung?.Ermid,
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

            // Falls ein Abo angegeben ist, weisen wir es dem neuen Kunden zu
            if (kunde.Abonr.HasValue && kunde.Abonr.Value > 0)
            {
                var start = DateTime.UtcNow;
                var end = start.AddYears(1);
                var selectedAbo = await _context.Abos.FindAsync(kunde.Abonr.Value);
                if (selectedAbo != null)
                {
                    int months = 12;
                    if (!string.IsNullOrEmpty(selectedAbo.Laufzeit))
                    {
                        var parts = selectedAbo.Laufzeit.Split('-');
                        if (parts.Length == 2 && int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m))
                        {
                            months = y * 12 + m;
                        }
                    }
                    end = start.AddMonths(months);
                }
                var kundenAbo = new TblKundenAbo
                {
                    Kundennr = kunde.Kundennr,
                    Abonr = kunde.Abonr.Value,
                    Startdatum = start,
                    Enddatum = end,
                    Status = "AKTIV"
                };
                _context.KundenAbos.Add(kundenAbo);
                await _context.SaveChangesAsync();

                // Falls auch eine Ermäßigung angegeben ist, erstellen wir direkt eine Abrechnung für den aktuellen Monat,
                // damit der Discount im System registriert wird.
                if (kunde.Ermid.HasValue && kunde.Ermid.Value > 0)
                {
                    var erm = await _context.Ermaessigte.FindAsync(kunde.Ermid.Value);
                    decimal discountRate = erm?.Ermaessigungssatz ?? 0m;
                    decimal originalPrice = selectedAbo?.Grundpreis ?? 0m;
                    decimal finalPrice = originalPrice * (1m - discountRate);

                    var abrechnung = new TblAbrechnung
                    {
                        Kundennr = kunde.Kundennr,
                        Abonr = kunde.Abonr.Value,
                        Ermid = kunde.Ermid.Value,
                        Kundenabonr = kundenAbo.Kundenabonr,
                        Rechnungsbetrag = finalPrice,
                        Abrechnungsmonat = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                    };
                    _context.Abrechnungen.Add(abrechnung);
                    await _context.SaveChangesAsync();
                }
            }

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

                // 1. Subscription (Abo) aktualisieren/anlegen
                TblKundenAbo? activeAbo = await _context.KundenAbos
                    .FirstOrDefaultAsync(ka => ka.Kundennr == id && ka.Status == "AKTIV");

                if (kunde.Abonr.HasValue && kunde.Abonr.Value > 0)
                {
                    if (activeAbo == null || activeAbo.Abonr != kunde.Abonr.Value)
                    {
                        if (activeAbo != null)
                        {
                            activeAbo.Status = "ABGELAUFEN";
                            activeAbo.Enddatum = DateTime.UtcNow;
                        }

                        var start = DateTime.UtcNow;
                        var end = start.AddYears(1);
                        var selectedAbo = await _context.Abos.FindAsync(kunde.Abonr.Value);
                        if (selectedAbo != null)
                        {
                            int months = 12;
                            if (!string.IsNullOrEmpty(selectedAbo.Laufzeit))
                            {
                                var parts = selectedAbo.Laufzeit.Split('-');
                                if (parts.Length == 2 && int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m))
                                {
                                    months = y * 12 + m;
                                }
                            }
                            end = start.AddMonths(months);
                        }

                        activeAbo = new TblKundenAbo
                        {
                            Kundennr = id,
                            Abonr = kunde.Abonr.Value,
                            Startdatum = start,
                            Enddatum = end,
                            Status = "AKTIV"
                        };
                        _context.KundenAbos.Add(activeAbo);
                        await _context.SaveChangesAsync();
                    }
                }
                else if (kunde.Abonr.HasValue && kunde.Abonr.Value == 0)
                {
                    // Abo explizit auf "Kein Abo" gesetzt
                    if (activeAbo != null)
                    {
                        activeAbo.Status = "ABGELAUFEN";
                        activeAbo.Enddatum = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        activeAbo = null;
                    }
                }

                // 2. Ermäßigung (Ermid) in der Abrechnung aktualisieren/anlegen
                if (kunde.Ermid.HasValue && kunde.Ermid.Value > 0)
                {
                    var latestAbrechnung = await _context.Abrechnungen
                        .Where(a => a.Kundennr == id)
                        .OrderByDescending(a => a.Abrechnungsmonat)
                        .FirstOrDefaultAsync();

                    var erm = await _context.Ermaessigte.FindAsync(kunde.Ermid.Value);
                    decimal discountRate = erm?.Ermaessigungssatz ?? 0m;

                    if (latestAbrechnung != null)
                    {
                        latestAbrechnung.Ermid = kunde.Ermid.Value;
                        var usedAbo = await _context.Abos.FindAsync(latestAbrechnung.Abonr);
                        latestAbrechnung.Rechnungsbetrag = (usedAbo?.Grundpreis ?? 0m) * (1m - discountRate);
                    }
                    else
                    {
                        int targetAboNr = activeAbo?.Abonr ?? (kunde.Abonr.HasValue && kunde.Abonr.Value > 0 ? kunde.Abonr.Value : 1);
                        var usedAbo = await _context.Abos.FindAsync(targetAboNr);
                        var newAbrechnung = new TblAbrechnung
                        {
                            Kundennr = id,
                            Abonr = targetAboNr,
                            Ermid = kunde.Ermid.Value,
                            Kundenabonr = activeAbo?.Kundenabonr,
                            Rechnungsbetrag = (usedAbo?.Grundpreis ?? 0m) * (1m - discountRate),
                            Abrechnungsmonat = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                        };
                        _context.Abrechnungen.Add(newAbrechnung);
                    }
                    await _context.SaveChangesAsync();
                }
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
