using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IntegraDentWallet.Data;
using IntegraDentWallet.Models;
using IntegraDentWallet.Services;

namespace IntegraDentWallet.Controllers;

[ApiController]
[Route("api/citas")]
public class CitasController : ControllerBase
{
    private readonly WalletDbContext _db;
    private readonly GoogleWalletService _wallet;
    private readonly PuntosService _puntos;

    public CitasController(WalletDbContext db, GoogleWalletService wallet, PuntosService puntos)
    {
        _db = db;
        _wallet = wallet;
        _puntos = puntos;
    }

    public record CrearCitaRequest(int PacienteId, DateTime FechaHora, string Motivo);

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearCitaRequest request)
    {
        var cita = new Cita
        {
            PacienteId = request.PacienteId,
            FechaHora = request.FechaHora,
            Motivo = request.Motivo
        };

        _db.Citas.Add(cita);
        await _db.SaveChangesAsync();

        await _wallet.ActualizarPaseAsync(request.PacienteId);

        return Ok(cita);
    }

    [HttpPost("{id}/completar")]
    public async Task<IActionResult> Completar(int id, [FromQuery] int puntosPorCita = 10)
    {
        await _puntos.MarcarCitaCompletadaYSumarPuntosAsync(id, puntosPorCita);
        return Ok();
    }

    [HttpGet("proximas")]
    public async Task<IActionResult> Proximas()
    {
        var citas = await _db.Citas
            .Include(c => c.Paciente)
            .Where(c => !c.Completada && c.FechaHora > DateTime.UtcNow)
            .OrderBy(c => c.FechaHora)
            .ToListAsync();

        return Ok(citas);
    }
}