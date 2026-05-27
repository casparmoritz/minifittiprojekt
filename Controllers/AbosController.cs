// ============================================================
// AbosController.cs – REST-API für die Abo-Verwaltung
// Route: /api/abos
// Unterstützt: GET (alle / einzeln), POST, PUT, DELETE
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharp_webapi.Data;
using csharp_webapi.Models;

namespace csharp_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AbosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AbosController(AppDbContext context)
        {
            _context = context;
        }

        // ── GET /api/abos ────────────────────────────────────────────────────
        // Gibt alle Abo-Typen zurück
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblAbo>>> GetAbos()
        {
            return await _context.Abos.ToListAsync();
        }

        // ── GET /api/abos/{id} ───────────────────────────────────────────────
        // Gibt ein einzelnes Abo anhand der AboNr zurück
        [HttpGet("{id}")]
        public async Task<ActionResult<TblAbo>> GetAbo(int id)
        {
            var abo = await _context.Abos.FindAsync(id);

            if (abo == null)
                return NotFound($"Abo mit AboNr {id} nicht gefunden.");

            return abo;
        }

        // ── POST /api/abos ───────────────────────────────────────────────────
        // Legt ein neues Abo an; AboNr wird von Oracle automatisch vergeben
        // Body-Beispiel:
        // {
        //   "kuendigsfrist": "2026-12-31T00:00:00Z",
        //   "kurs": 1, "getraenke": 1,
        //   "grundpreis": 49.99, "laufzeit": "1-0"
        // }
        [HttpPost]
        public async Task<ActionResult<TblAbo>> PostAbo(TblAbo abo)
        {
            _context.Abos.Add(abo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAbo), new { id = abo.Abonr }, abo);
        }

        // ── PUT /api/abos/{id} ───────────────────────────────────────────────
        // Aktualisiert ein bestehendes Abo
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAbo(int id, TblAbo abo)
        {
            if (id != abo.Abonr)
                return BadRequest("AboNr in der URL stimmt nicht mit dem Body überein.");

            _context.Entry(abo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Abos.AnyAsync(a => a.Abonr == id))
                    return NotFound($"Abo mit AboNr {id} nicht gefunden.");
                throw;
            }

            return NoContent();
        }

        // ── DELETE /api/abos/{id} ────────────────────────────────────────────
        // Löscht ein Abo (nur möglich wenn keine Abrechnungen/KundenAbos verknüpft)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAbo(int id)
        {
            var abo = await _context.Abos.FindAsync(id);
            if (abo == null)
                return NotFound($"Abo mit AboNr {id} nicht gefunden.");

            _context.Abos.Remove(abo);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
