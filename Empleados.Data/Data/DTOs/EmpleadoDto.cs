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
    public decimal Salario { get; set; }
    public DateTime FechaContratacion { get; set; }
    public DateTime? FechaTerminacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? FotoRuta { get; set; }
    public string NombreDepartamento { get; set; } = string.Empty;
    public int? PuestoId { get; set; }
    public string? NombrePuesto { get; set; }

    // Requiere Departamento y PuestoNav cargados
    public static EmpleadoDto Desde(Entities.Empleado e) => new()
    {
        Id = e.Id,
        Nombre = e.Nombre,
        Apellido = e.Apellido,
        FechaNacimiento = e.FechaNacimiento,
        Direccion = e.Direccion,
        Telefono = e.Telefono,
        CorreoElectronico = e.CorreoElectronico,
        Salario = e.Salario,
        FechaContratacion = e.FechaContratacion,
        FechaTerminacion = e.FechaTerminacion,
        Estado = e.Estado.ToString(),
        FotoRuta = e.FotoRuta,
        NombreDepartamento = e.Departamento?.Nombre ?? "",
        PuestoId = e.PuestoId,
        NombrePuesto = e.PuestoNav?.Nombre
    };
}