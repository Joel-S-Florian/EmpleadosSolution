namespace Empleados.Data.Entities;

public enum EstadoEmpleado
{
    Activo,
    Inactivo,
    Despedido
}

public class Empleado
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
    public EstadoEmpleado Estado { get; set; } = EstadoEmpleado.Activo;
    public string? FotoRuta { get; set; }

    public int DepartamentoId { get; set; }
    public virtual Departamento? Departamento { get; set; }

    public int? PuestoId { get; set; }
    public virtual Puesto? PuestoNav { get; set; }
}