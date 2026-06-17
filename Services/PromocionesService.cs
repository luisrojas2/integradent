using IntegraDentWallet.Data;
using IntegraDentWallet.Models;
using Microsoft.EntityFrameworkCore;

namespace IntegraDentWallet.Services;

public class PromocionesService
{
    private readonly WalletDbContext _db;
    private readonly GoogleWalletService _wallet;

    public PromocionesService(WalletDbContext db, GoogleWalletService wallet)
    {
        _db = db;
        _wallet = wallet;
    }

    public async Task<Promocion> CrearPromocionAsync(
        string titulo, string descripcion, DateTime fechaInicio, DateTime fechaFin,
        bool paraTodos, List<int>? pacientesEspecificos = null)
    {
        var promo = new Promocion
        {
            Titulo = titulo,
            Descripcion = descripcion,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            ParaTodos = paraTodos,
            Activa = true
        };

        _db.Promociones.Add(promo);
        await _db.SaveChangesAsync();

        if (!paraTodos && pacientesEspecificos != null)
        {
            foreach (var pacienteId in pacientesEspecificos)
            {
                _db.PacientePromociones.Add(new PacientePromocion
                {
                    PacienteId = pacienteId,
                    PromocionId = promo.Id
                });
            }
            await _db.SaveChangesAsync();
        }

        var idsAfectados = paraTodos
            ? await _db.Pacientes.Where(p => p.TienePaseGoogle).Select(p => p.Id).ToListAsync()
            : pacientesEspecificos ?? new List<int>();

        foreach (var id in idsAfectados)
        {
            await _wallet.ActualizarPaseAsync(id);
        }

        return promo;
    }

    public async Task DesactivarPromocionAsync(int promocionId)
    {
        var promo = await _db.Promociones
            .Include(p => p.PacientePromociones)
            .FirstOrDefaultAsync(p => p.Id == promocionId)
            ?? throw new Exception("Promocion no encontrada");

        promo.Activa = false;
        await _db.SaveChangesAsync();

        var idsAfectados = promo.ParaTodos
            ? await _db.Pacientes.Where(p => p.TienePaseGoogle).Select(p => p.Id).ToListAsync()
            : promo.PacientePromociones.Select(pp => pp.PacienteId).ToList();

        foreach (var id in idsAfectados)
        {
            await _wallet.ActualizarPaseAsync(id);
        }
    }
}