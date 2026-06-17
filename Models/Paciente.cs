namespace IntegraDentWallet.Models;

public class Paciente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Puntos { get; set; } = 0;

    // Se llena la primera vez que el paciente agrega el pase a su Wallet.
    public bool TienePaseGoogle { get; set; } = false;

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}