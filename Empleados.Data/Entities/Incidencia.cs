using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.Entities;

public enum TipoIncidencia
{
    Falta,
    Retardo,
    [Display(Name = "Permiso con goce")] PermisoConGoce,
    [Display(Name = "Permiso sin goce")] PermisoSinGoce,
    [Display(Name = "Licencia médica")] LicenciaMedica,
    [Display(Name = "Licencia de maternidad")] LicenciaMaternidad,
    [Display(Name = "Licencia de paternidad")] LicenciaPaternidad,
    [Display(Name = "Licencia por luto")] LicenciaLuto,
    [Display(Name = "Licencia por matrimonio")] LicenciaMatrimonio,
    Vacaciones
}

public enum EstadoIncidencia
{
    [Display(Name = "Por aprobar")] Pendiente,
    Aprobada,
    Rechazada
}

// Ausencias, retardos, permisos, licencias y vacaciones. Las aprobadas con descuento entran solas en la nómina.
public class Incidencia
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }

    public TipoIncidencia Tipo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    // Días laborables (calculados o ajustados a mano); en retardos se usan las horas
    public decimal Dias { get; set; }
    public decimal Horas { get; set; }
    // Se descuenta del salario en la nómina del período
    public bool Descontar { get; set; }

    public EstadoIncidencia Estado { get; set; }
    public string? Comentario { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? CreadaPor { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? RevisadaPor { get; set; }
    public DateTime? FechaRevision { get; set; }

    public static bool DescuentaPorDefecto(TipoIncidencia tipo) =>
        tipo is TipoIncidencia.Falta or TipoIncidencia.Retardo or TipoIncidencia.PermisoSinGoce;
}
