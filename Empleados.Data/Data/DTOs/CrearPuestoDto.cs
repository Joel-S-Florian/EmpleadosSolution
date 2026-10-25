using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.DTOs;

public class CrearPuestoDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede tener más de 500 caracteres")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El departamento es obligatorio")]
    public int DepartamentoId { get; set; }

    [Range(0.01, 99999999.99, ErrorMessage = "El salario mínimo debe ser mayor que 0")]
    public decimal? SalarioMinimo { get; set; }

    [Range(0.01, 99999999.99, ErrorMessage = "El salario máximo debe ser mayor que 0")]
    public decimal? SalarioMaximo { get; set; }
}
