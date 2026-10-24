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
    public DbSet<Puesto> Puestos { get; set; }
    public DbSet<Notificacion> Notificaciones { get; set; }

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

        // Configuración de Puesto
        modelBuilder.Entity<Puesto>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Descripcion).HasMaxLength(500);
        });

        // Relación Empleado - Departamento
        modelBuilder.Entity<Empleado>()
            .HasOne(e => e.Departamento)
            .WithMany(d => d.Empleados)
            .HasForeignKey(e => e.DepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación Puesto - Departamento
        modelBuilder.Entity<Puesto>()
            .HasOne(p => p.Departamento)
            .WithMany(d => d.Puestos)
            .HasForeignKey(p => p.DepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación Empleado - Puesto (opcional, integridad referencial)
        modelBuilder.Entity<Empleado>()
            .HasOne(e => e.PuestoNav)
            .WithMany(p => p.Empleados)
            .HasForeignKey(e => e.PuestoId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configuración de Notificacion
        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Mensaje).IsRequired().HasMaxLength(500);
            entity.Property(n => n.Tipo).HasConversion<string>().HasMaxLength(20);
            entity.Property(n => n.UsuarioDestinoId).HasMaxLength(450);
            entity.HasIndex(n => n.UsuarioDestinoId);
            entity.Property(n => n.FechaCreacion).IsRequired();
        });
    }
}