namespace IntegraDentWallet.Models;

public class Promocion
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public bool Activa { get; set; } = true;

    public bool ParaTodos { get; set; } = true;

    public ICollection<PacientePromocion> PacientePromociones { get; set; } = new List<PacientePromocion>();
}

public class PacientePromocion
{
    public int PacienteId { get; set; }
    public Paciente? Paciente { get; set; }

    public int PromocionId { get; set; }
    public Promocion? Promocion { get; set; }
}