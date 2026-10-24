using Empleados.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.DTOs;

public class CrearEmpleadoDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(100, ErrorMessage = "El apellido no puede tener más de 100 caracteres")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
    public DateTime FechaNacimiento { get; set; }

    [Required(ErrorMessage = "La dirección es obligatoria")]
    [StringLength(255)]
    public string Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [StringLength(20)]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
    [StringLength(255)]
    public string CorreoElectronico { get; set; } = string.Empty;

    [Required(ErrorMessage = "El salario es obligatorio")]
    [Range(0.01, 99999999.99, ErrorMessage = "El salario debe ser mayor que 0")]
    public decimal Salario { get; set; }

    [Required(ErrorMessage = "La fecha de contratación es obligatoria")]
    public DateTime FechaContratacion { get; set; }

    [Required(ErrorMessage = "El departamento es obligatorio")]
    public int DepartamentoId { get; set; }

    public int? PuestoId { get; set; }

    // Datos de nómina. Opcionales por ahora; el cálculo de nómina (Fase 3.1) exigirá documento y NSS.
    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Cedula;

    [StringLength(20)]
    public string? NumeroDocumento { get; set; }

    [StringLength(20)]
    public string? Nss { get; set; }

    public Sexo? Sexo { get; set; }

    [StringLength(100)]
    public string? Afp { get; set; }

    [StringLength(100)]
    public string? Ars { get; set; }

    [Range(0, 20, ErrorMessage = "Los dependientes adicionales deben estar entre 0 y 20")]
    public int DependientesSfs { get; set; }

    public TipoContrato TipoContrato { get; set; } = TipoContrato.Indefinido;

    public DateTime? FechaFinContrato { get; set; }

    // Si no se indica, se usa la frecuencia predeterminada de la empresa
    public FrecuenciaPago? FrecuenciaPago { get; set; }

    public TipoSalario TipoSalario { get; set; } = TipoSalario.FijoMensual;

    public int? BancoId { get; set; }

    [StringLength(30)]
    public string? NumeroCuenta { get; set; }

    public TipoCuenta? TipoCuenta { get; set; }
}