using IntegraDentWallet.Data;

namespace IntegraDentWallet.Services;

public class PuntosService
{
    private readonly WalletDbContext _db;
    private readonly GoogleWalletService _wallet;

    public PuntosService(WalletDbContext db, GoogleWalletService wallet)
    {
        _db = db;
        _wallet = wallet;
    }

    public async Task SumarPuntosAsync(int pacienteId, int cantidad)
    {
        var paciente = await _db.Pacientes.FindAsync(pacienteId)
            ?? throw new Exception("Paciente no encontrado");

        paciente.Puntos += cantidad;
        await _db.SaveChangesAsync();

        await _wallet.ActualizarPaseAsync(pacienteId);
    }

    public async Task MarcarCitaCompletadaYSumarPuntosAsync(int citaId, int puntosPorCita = 10)
    {
        var cita = await _db.Citas.FindAsync(citaId)
            ?? throw new Exception("Cita no encontrada");

        if (cita.PuntosOtorgados)
        {
            return;
        }

        cita.Completada = true;
        cita.PuntosOtorgados = true;
        await _db.SaveChangesAsync();

        await SumarPuntosAsync(cita.PacienteId, puntosPorCita);
    }
}