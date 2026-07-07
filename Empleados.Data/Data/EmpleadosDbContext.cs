using Empleados.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Data;

public class EmpleadosDbContext : IdentityDbContext<IdentityUser>
{
    public EmpleadosDbContext(DbContextOptions<EmpleadosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<Departamento> Departamentos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de Empleado
        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Apellido).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CorreoElectronico).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.CorreoElectronico).IsUnique();
            entity.Property(e => e.Telefono).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Direccion).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Puesto).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Salario).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.FotoRuta).HasMaxLength(500);
        });

        // Configuración de Departamento
        modelBuilder.Entity<Departamento>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Descripcion).HasMaxLength(500);
        });

        // Relación
        modelBuilder.Entity<Empleado>()
            .HasOne(e => e.Departamento)
            .WithMany(d => d.Empleados)
            .HasForeignKey(e => e.DepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}