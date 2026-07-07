namespace Empleados.Data.DTOs;

public class EmpleadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
    public decimal Salario { get; set; }
    public DateTime FechaContratacion { get; set; }
    public DateTime? FechaTerminacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? FotoRuta { get; set; }
    public string NombreDepartamento { get; set; } = string.Empty;
}