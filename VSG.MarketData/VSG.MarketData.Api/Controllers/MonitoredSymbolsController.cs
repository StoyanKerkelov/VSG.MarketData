
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VSG.MarketData.Core.Models;
using VSG.MarketData.Infrastructure.Persistence;

namespace VSG.MarketData.Api.Controllers;

[ApiController]
[Route("api/admin/symbols")]
public class MonitoredSymbolsController(MarketDataDbContext dbContext)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MonitoredSymbol>>> GetAll(
        CancellationToken cancellationToken)
    {
        var symbols = await dbContext.MonitoredSymbols.AsNoTracking()
                                                      .OrderBy(x => x.Symbol)
                                                      .ToListAsync(cancellationToken);

        return Ok(symbols);
    }

    [HttpPost]
    public async Task<ActionResult<MonitoredSymbol>> Add(
        [FromBody] string symbol,
        CancellationToken cancellationToken)
    {
        symbol = symbol.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(symbol) ||
            symbol.Length > 20 ||
            !symbol.All(char.IsAsciiLetterOrDigit))
        {
            return BadRequest("Invalid symbol.");
        }

        var exists = await dbContext.MonitoredSymbols
            .AnyAsync(x => x.Symbol == symbol, cancellationToken);

        if (exists)
        {
            return Conflict($"Symbol '{symbol}' already exists.");
        }

        var monitoredSymbol = new MonitoredSymbol
        {
            Symbol = symbol,
            IsActive = true
        };

        dbContext.MonitoredSymbols.Add(monitoredSymbol);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), monitoredSymbol);
    }

    [HttpPatch("{symbol}/active")]
    public async Task<IActionResult> SetActive(
        string symbol,
        [FromQuery] bool active,
        CancellationToken cancellationToken)
    {
        symbol = symbol.Trim().ToUpperInvariant();

        var monitoredSymbol = await dbContext.MonitoredSymbols.SingleOrDefaultAsync(x => x.Symbol == symbol, cancellationToken);

        if (monitoredSymbol is null)
        {
            return NotFound();
        }

        monitoredSymbol.IsActive = active;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
