using Empleados.Data.Entities;

namespace Empleados.Mvc.Models;

// Lo pendiente de la empresa actual para el inicio del sistema
public class ResumenInicio
{
    public int EmpleadosActivos { get; set; }
    public List<NominaPeriodo> NominasPorAprobar { get; set; } = new();
    public int IncidenciasPorAprobar { get; set; }
    public int LiquidacionesBorrador { get; set; }
    public NominaPeriodo? UltimaPagada { get; set; }
    public List<Feriado> ProximosFeriados { get; set; } = new();
}
