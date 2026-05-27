using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharp_webapi.Data;
using csharp_webapi.Models;

namespace csharp_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BankenController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BankenController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblBank>>> GetBanken()
        {
            return await _context.Banken.ToListAsync();
        }

        [HttpGet("{bic}")]
        public async Task<ActionResult<TblBank>> GetBank(string bic)
        {
            var bank = await _context.Banken.FindAsync(bic);

            if (bank == null)
            {
                return NotFound();
            }

            return bank;
        }

        [HttpPost]
        public async Task<ActionResult<TblBank>> PostBank(TblBank bank)
        {
            if (await _context.Banken.AnyAsync(b => b.Bic == bank.Bic))
            {
                return Conflict("Eine Bank mit dieser BIC existiert bereits.");
            }

            _context.Banken.Add(bank);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBank), new { bic = bank.Bic }, bank);
        }

        [HttpPut("{bic}")]
        public async Task<IActionResult> PutBank(string bic, TblBank bank)
        {
            if (bic != bank.Bic)
            {
                return BadRequest("BIC in URL stimmt nicht mit der BIC im Body überein.");
            }

            _context.Entry(bank).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Banken.AnyAsync(b => b.Bic == bic))
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

        [HttpDelete("{bic}")]
        public async Task<IActionResult> DeleteBank(string bic)
        {
            var bank = await _context.Banken.FindAsync(bic);
            if (bank == null)
            {
                return NotFound();
            }

            _context.Banken.Remove(bank);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
