// ============================================================
// ErmaessigteController.cs – REST-API für Ermäßigungssätze
// Route: /api/ermaessigungen
// Dieser Controller fehlte im Original-Projekt komplett!
// Das Frontend fragt /api/ermaessigungen ab – ohne diesen Controller
// schlug die Verbindung fehl und es wurden Testdaten verwendet.
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharp_webapi.Data;
using csharp_webapi.Models;

namespace csharp_webapi.Controllers
{
    [ApiController]
    [Route("api/ermaessigungen")] // Route explizit gesetzt (nicht "ermaessigte")
    public class ErmaessigteController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ErmaessigteController(AppDbContext context)
        {
            _context = context;
        }

        // ── GET /api/ermaessigungen ──────────────────────────────────────────
        // Gibt alle Ermäßigungssätze zurück
        // Wird von App.vue beim Start geladen (ladeStammdaten)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblErmaessigte>>> GetErmaessigungen()
        {
            return await _context.Ermaessigte.ToListAsync();
        }

        // ── GET /api/ermaessigungen/{id} ─────────────────────────────────────
        // Gibt eine einzelne Ermäßigung anhand der ErmID zurück
        [HttpGet("{id}")]
        public async Task<ActionResult<TblErmaessigte>> GetErmaessigung(int id)
        {
            var erm = await _context.Ermaessigte.FindAsync(id);

            if (erm == null)
                return NotFound($"Ermäßigung mit ErmID {id} nicht gefunden.");

            return erm;
        }

        // ── POST /api/ermaessigungen ─────────────────────────────────────────
        // Legt eine neue Ermäßigung an
        // Body-Beispiel: { "ermaessigungssatz": 0.15 }  → 15% Ermäßigung
        [HttpPost]
        public async Task<ActionResult<TblErmaessigte>> PostErmaessigung(TblErmaessigte erm)
        {
            _context.Ermaessigte.Add(erm);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetErmaessigung), new { id = erm.Ermid }, erm);
        }

        // ── PUT /api/ermaessigungen/{id} ─────────────────────────────────────
        // Aktualisiert einen bestehenden Ermäßigungssatz
        [HttpPut("{id}")]
        public async Task<IActionResult> PutErmaessigung(int id, TblErmaessigte erm)
        {
            if (id != erm.Ermid)
                return BadRequest("ErmID in der URL stimmt nicht mit dem Body überein.");

            _context.Entry(erm).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Ermaessigte.AnyAsync(e => e.Ermid == id))
                    return NotFound($"Ermäßigung mit ErmID {id} nicht gefunden.");
                throw;
            }

            return NoContent();
        }

        // ── DELETE /api/ermaessigungen/{id} ──────────────────────────────────
        // Löscht eine Ermäßigung (nur wenn keine Abrechnungen darauf verweisen)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteErmaessigung(int id)
        {
            var erm = await _context.Ermaessigte.FindAsync(id);
            if (erm == null)
                return NotFound($"Ermäßigung mit ErmID {id} nicht gefunden.");

            _context.Ermaessigte.Remove(erm);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
