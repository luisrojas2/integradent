namespace IntegraDentWallet.Models;

public class Cita
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public Paciente? Paciente { get; set; }

    public DateTime FechaHora { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public bool Completada { get; set; } = false;

    // Si ya se le sumaron puntos por esta cita (para no duplicar)
    public bool PuntosOtorgados { get; set; } = false;
}