namespace AuditoriaDocumental.Api.Datos;

using Microsoft.EntityFrameworkCore;
using AuditoriaDocumental.Api.Modelos;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // estas son las tablas que se van a crear en sql
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Documento> Documentos { get; set; }
    public DbSet<Extraccion> Extracciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // relacion de uno a uno entre el documento y lo que lee la ia
        modelBuilder.Entity<Documento>()
            .HasOne(d => d.Extraccion)
            .WithOne(e => e.Documento)
            .HasForeignKey<Extraccion>(e => e.DocumentoId);
            
        // relacion de uno a muchos: un auditor revisa varios documentos
        modelBuilder.Entity<Documento>()
            .HasOne(d => d.Auditor)
            .WithMany(u => u.Documentos)
            .HasForeignKey(d => d.AuditorId);

        // le decimos a sql que el importe tendrá máximo 18 números y 2 decimales
        modelBuilder.Entity<Extraccion>()
            .Property(e => e.ImporteTotal)
            .HasPrecision(18, 2);
    }
}