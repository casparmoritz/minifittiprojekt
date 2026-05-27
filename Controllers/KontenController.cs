using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharp_webapi.Data;
using csharp_webapi.Models;

namespace csharp_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KontenController : ControllerBase
    {
        private readonly AppDbContext _context;

        public KontenController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblKonto>>> GetKonten()
        {
            return await _context.Konten.Include(k => k.Bank).ToListAsync();
        }

        [HttpGet("{iban}")]
        public async Task<ActionResult<TblKonto>> GetKonto(string iban)
        {
            var konto = await _context.Konten.Include(k => k.Bank).FirstOrDefaultAsync(k => k.Iban == iban);

            if (konto == null)
            {
                return NotFound();
            }

            return konto;
        }

        [HttpPost]
        public async Task<ActionResult<TblKonto>> PostKonto(TblKonto konto)
        {
            if (await _context.Konten.AnyAsync(k => k.Iban == konto.Iban))
            {
                return Conflict("Ein Konto mit dieser IBAN existiert bereits.");
            }

            if (!await _context.Banken.AnyAsync(b => b.Bic == konto.Bic))
            {
                return BadRequest("Die angegebene BIC existiert nicht.");
            }

            // Clear navigation property so EF Core doesn't try to insert a new Bank
            konto.Bank = null;

            _context.Konten.Add(konto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetKonto), new { iban = konto.Iban }, konto);
        }

        [HttpPut("{iban}")]
        public async Task<IActionResult> PutKonto(string iban, TblKonto konto)
        {
            if (iban != konto.Iban)
            {
                return BadRequest("IBAN in URL stimmt nicht mit der IBAN im Body überein.");
            }

            if (!await _context.Banken.AnyAsync(b => b.Bic == konto.Bic))
            {
                return BadRequest("Die angegebene BIC existiert nicht.");
            }

            konto.Bank = null;
            _context.Entry(konto).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Konten.AnyAsync(k => k.Iban == iban))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{iban}")]
        public async Task<IActionResult> DeleteKonto(string iban)
        {
            var konto = await _context.Konten.FindAsync(iban);
            if (konto == null)
            {
                return NotFound();
            }

            _context.Konten.Remove(konto);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
