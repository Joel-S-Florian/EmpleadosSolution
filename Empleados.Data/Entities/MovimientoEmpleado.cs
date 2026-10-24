using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.Entities;

public enum TipoMovimiento
{
    Alta,
    [Display(Name = "Cambio de salario")] CambioSalario,
    [Display(Name = "Cambio de puesto")] CambioPuesto,
    [Display(Name = "Cambio de departamento")] CambioDepartamento,
    [Display(Name = "Cambio de frecuencia de pago")] CambioFrecuenciaPago,
    [Display(Name = "Desactivación")] Desactivacion,
    Despido
}

// Historial inmutable de cambios relevantes para nómina y RRHH
public class MovimientoEmpleado
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int EmpleadoId { get; set; }
    public virtual Empleado? Empleado { get; set; }
    public DateTime Fecha { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? UsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
}
