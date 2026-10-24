using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.Entities;

// Define el salario mínimo aplicable (sector privado no sectorizado)
public enum TamanoEmpresa
{
    Micro,
    [Display(Name = "Pequeña")] Pequena,
    Mediana,
    Grande
}

public enum FrecuenciaPago
{
    Quincenal,
    Mensual
}

public class Empresa
{
    public int Id { get; set; }
    public string Rnc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public TamanoEmpresa Tamano { get; set; } = TamanoEmpresa.Grande;
    // Porcentaje del Seguro de Riesgos Laborales que asigna la TSS a la empresa (cuota básica + variable)
    public decimal TasaRiesgoSrl { get; set; } = 1.20m;
    public FrecuenciaPago FrecuenciaPagoPredeterminada { get; set; } = FrecuenciaPago.Quincenal;
    public int MesCierreFiscal { get; set; } = 12;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? CorreoElectronico { get; set; }
    public bool Activa { get; set; } = true;

    public virtual ICollection<UsuarioEmpresa> Usuarios { get; set; } = new List<UsuarioEmpresa>();
}

// Empresas a las que tiene acceso cada usuario de Identity
public class UsuarioEmpresa
{
    public string UsuarioId { get; set; } = string.Empty;
    public int EmpresaId { get; set; }
    public virtual Empresa? Empresa { get; set; }
}
