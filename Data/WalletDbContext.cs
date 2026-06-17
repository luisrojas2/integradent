using Microsoft.EntityFrameworkCore;
using IntegraDentWallet.Models;

namespace IntegraDentWallet.Data;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options) { }

    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Promocion> Promociones => Set<Promocion>();
    public DbSet<PacientePromocion> PacientePromociones => Set<PacientePromocion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PacientePromocion>()
            .HasKey(pp => new { pp.PacienteId, pp.PromocionId });

        modelBuilder.Entity<PacientePromocion>()
            .HasOne(pp => pp.Paciente)
            .WithMany()
            .HasForeignKey(pp => pp.PacienteId);

        modelBuilder.Entity<PacientePromocion>()
            .HasOne(pp => pp.Promocion)
            .WithMany(p => p.PacientePromociones)
            .HasForeignKey(pp => pp.PromocionId);

        modelBuilder.Entity<Cita>()
            .HasOne(c => c.Paciente)
            .WithMany(p => p.Citas)
            .HasForeignKey(c => c.PacienteId);
    }
}