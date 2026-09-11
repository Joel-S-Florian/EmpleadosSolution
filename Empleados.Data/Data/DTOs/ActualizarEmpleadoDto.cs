using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.DTOs;

public class ActualizarEmpleadoDto
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

    [Required(ErrorMessage = "El puesto es obligatorio")]
    [StringLength(100)]
    public string Puesto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El salario es obligatorio")]
    [Range(0, 999999.99, ErrorMessage = "El salario debe ser mayor a 0")]
    public decimal Salario { get; set; }

    [Required(ErrorMessage = "La fecha de contratación es obligatoria")]
    public DateTime FechaContratacion { get; set; }

    [Required(ErrorMessage = "El departamento es obligatorio")]
    public int DepartamentoId { get; set; }

    public int? PuestoId { get; set; }
}