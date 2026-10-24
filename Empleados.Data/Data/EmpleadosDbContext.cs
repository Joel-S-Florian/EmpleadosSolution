using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Data;

public class EmpleadosDbContext : IdentityDbContext<IdentityUser>
{
    private readonly IContextoUsuario? _contexto;

    // Sin contexto (tests, herramientas) no se filtra por empresa
    public EmpleadosDbContext(DbContextOptions<EmpleadosDbContext> options, IContextoUsuario? contexto = null)
        : base(options)
    {
        _contexto = contexto;
    }

    // Usados por los filtros globales; EF los evalúa en cada consulta
    private bool FiltrarPorEmpresa => _contexto is { EsSistema: false };
    private int? EmpresaActualId => _contexto?.EmpresaId;

    public DbSet<Empresa> Empresas { get; set; }
    public DbSet<UsuarioEmpresa> UsuariosEmpresas { get; set; }
    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<Departamento> Departamentos { get; set; }
    public DbSet<Puesto> Puestos { get; set; }
    public DbSet<Notificacion> Notificaciones { get; set; }
    public DbSet<Banco> Bancos { get; set; }
    public DbSet<MovimientoEmpleado> MovimientosEmpleados { get; set; }
    public DbSet<ParametroLegal> ParametrosLegales { get; set; }
    public DbSet<TramoIsr> TramosIsr { get; set; }
    public DbSet<SalarioMinimo> SalariosMinimos { get; set; }
    public DbSet<Feriado> Feriados { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Rnc).IsRequired().HasMaxLength(11);
            entity.HasIndex(e => e.Rnc).IsUnique();
            entity.Property(e => e.RazonSocial).IsRequired().HasMaxLength(200);
            entity.Property(e => e.NombreComercial).HasMaxLength(200);
            entity.Property(e => e.Tamano).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TasaRiesgoSrl).HasPrecision(5, 2);
            entity.Property(e => e.FrecuenciaPagoPredeterminada).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Direccion).HasMaxLength(255);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.CorreoElectronico).HasMaxLength(255);
        });

        modelBuilder.Entity<UsuarioEmpresa>(entity =>
        {
            entity.HasKey(u => new { u.UsuarioId, u.EmpresaId });
            entity.HasOne(u => u.Empresa).WithMany(e => e.Usuarios).HasForeignKey(u => u.EmpresaId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<IdentityUser>().WithMany().HasForeignKey(u => u.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de Empleado
        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Apellido).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CorreoElectronico).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => new { e.EmpresaId, e.CorreoElectronico }).IsUnique();
            entity.Property(e => e.Telefono).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Direccion).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Salario).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.FotoRuta).HasMaxLength(500);
            entity.Property(e => e.TipoDocumento).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.NumeroDocumento).HasMaxLength(20);
            entity.HasIndex(e => new { e.EmpresaId, e.NumeroDocumento }).IsUnique().HasFilter("[NumeroDocumento] IS NOT NULL");
            entity.Property(e => e.Nss).HasMaxLength(20);
            entity.Property(e => e.Sexo).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Afp).HasMaxLength(100);
            entity.Property(e => e.Ars).HasMaxLength(100);
            entity.Property(e => e.TipoContrato).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.FrecuenciaPago).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TipoSalario).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.NumeroCuenta).HasMaxLength(30);
            entity.Property(e => e.TipoCuenta).HasConversion<string>().HasMaxLength(20);
            entity.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Banco).WithMany().HasForeignKey(e => e.BancoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !FiltrarPorEmpresa || e.EmpresaId == EmpresaActualId);
        });

        // Configuración de Departamento
        modelBuilder.Entity<Departamento>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Descripcion).HasMaxLength(500);
            entity.HasOne<Empresa>().WithMany().HasForeignKey(d => d.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(d => !FiltrarPorEmpresa || d.EmpresaId == EmpresaActualId);
        });

        // Configuración de Puesto
        modelBuilder.Entity<Puesto>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Descripcion).HasMaxLength(500);
            entity.HasOne<Empresa>().WithMany().HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(p => !FiltrarPorEmpresa || p.EmpresaId == EmpresaActualId);
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

        modelBuilder.Entity<MovimientoEmpleado>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(40);
            entity.Property(m => m.ValorAnterior).HasMaxLength(200);
            entity.Property(m => m.ValorNuevo).HasMaxLength(200);
            entity.Property(m => m.UsuarioId).HasMaxLength(450);
            entity.Property(m => m.UsuarioNombre).HasMaxLength(256);
            entity.HasIndex(m => new { m.EmpleadoId, m.Fecha });
            entity.HasOne(m => m.Empleado).WithMany().HasForeignKey(m => m.EmpleadoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(m => !FiltrarPorEmpresa || m.EmpresaId == EmpresaActualId);
        });

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

        modelBuilder.Entity<Banco>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Codigo).IsRequired().HasMaxLength(20);
            entity.HasIndex(b => b.Codigo).IsUnique();
            entity.Property(b => b.Nombre).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<ParametroLegal>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Clave).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Valor).HasPrecision(18, 4);
            entity.Property(p => p.Fuente).HasMaxLength(300);
            entity.HasIndex(p => new { p.Clave, p.VigenteDesde }).IsUnique();
        });

        modelBuilder.Entity<TramoIsr>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Desde).HasPrecision(18, 2);
            entity.Property(t => t.Hasta).HasPrecision(18, 2);
            entity.Property(t => t.Tasa).HasPrecision(5, 2);
            entity.Property(t => t.CuotaFija).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SalarioMinimo>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Tamano).HasConversion<string>().HasMaxLength(20);
            entity.Property(s => s.Monto).HasPrecision(18, 2);
            entity.HasIndex(s => new { s.Tamano, s.VigenteDesde }).IsUnique();
        });

        modelBuilder.Entity<Feriado>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Nombre).IsRequired().HasMaxLength(100);
            entity.HasIndex(f => f.Fecha).IsUnique();
        });

        DatosLegales.Sembrar(modelBuilder);
    }
}
