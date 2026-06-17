using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IntegraDentWallet.Data;
using IntegraDentWallet.Services;

namespace IntegraDentWallet.Controllers;

[ApiController]
[Route("api/promociones")]
public class PromocionesController : ControllerBase
{
    private readonly WalletDbContext _db;
    private readonly PromocionesService _promociones;

    public PromocionesController(WalletDbContext db, PromocionesService promociones)
    {
        _db = db;
        _promociones = promociones;
    }

    public record CrearPromocionRequest(
        string Titulo, string Descripcion, DateTime FechaInicio, DateTime FechaFin,
        bool ParaTodos, List<int>? PacientesEspecificos);

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var promos = await _db.Promociones.OrderByDescending(p => p.FechaInicio).ToListAsync();
        return Ok(promos);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearPromocionRequest request)
    {
        var promo = await _promociones.CrearPromocionAsync(
            request.Titulo, request.Descripcion, request.FechaInicio, request.FechaFin,
            request.ParaTodos, request.PacientesEspecificos);

        return Ok(promo);
    }

    [HttpPost("{id}/desactivar")]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _promociones.DesactivarPromocionAsync(id);
        return Ok();
    }
}