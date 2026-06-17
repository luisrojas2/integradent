using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IntegraDentWallet.Data;
using IntegraDentWallet.Models;
using IntegraDentWallet.Services;

namespace IntegraDentWallet.Controllers;

[ApiController]
[Route("api/pacientes")]
public class PacientesController : ControllerBase
{
    private readonly WalletDbContext _db;
    private readonly GoogleWalletService _wallet;
    private readonly PuntosService _puntos;

    public PacientesController(WalletDbContext db, GoogleWalletService wallet, PuntosService puntos)
    {
        _db = db;
        _wallet = wallet;
        _puntos = puntos;
    }

    public record CrearPacienteRequest(string Nombre, string Telefono, string? Email);
    public record SumarPuntosRequest(int Cantidad);

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? buscar)
    {
        var query = _db.Pacientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(p => p.Nombre.Contains(buscar) || p.Telefono.Contains(buscar));
        }

        var pacientes = await query.OrderBy(p => p.Nombre).ToListAsync();
        return Ok(pacientes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var paciente = await _db.Pacientes
            .Include(p => p.Citas)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (paciente == null) return NotFound();
        return Ok(paciente);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearPacienteRequest request)
    {
        var paciente = new Paciente
        {
            Nombre = request.Nombre,
            Telefono = request.Telefono,
            Email = request.Email
        };

        _db.Pacientes.Add(paciente);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Obtener), new { id = paciente.Id }, paciente);
    }

    [HttpPost("{id}/puntos")]
    public async Task<IActionResult> SumarPuntos(int id, [FromBody] SumarPuntosRequest request)
    {
        await _puntos.SumarPuntosAsync(id, request.Cantidad);
        return Ok();
    }

    [HttpGet("{id}/google-wallet-link")]
    public async Task<IActionResult> ObtenerLinkGoogleWallet(int id)
    {
        var paciente = await _db.Pacientes
            .Include(p => p.Citas)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (paciente == null) return NotFound();

        var url = await _wallet.GenerarLinkDeWalletAsync(paciente);
        return Ok(new { url });
    }
}