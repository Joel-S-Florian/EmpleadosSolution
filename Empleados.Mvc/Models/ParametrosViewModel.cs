using Empleados.Data.Entities;

namespace Empleados.Mvc.Models;

public class ParametrosViewModel
{
    public DateTime Fecha { get; set; }
    public List<ParametroLegal> Vigentes { get; set; } = new();
    public List<ParametroLegal> Historial { get; set; } = new();
    public List<TramoIsr> EscalaIsr { get; set; } = new();
    public List<SalarioMinimo> SalariosMinimos { get; set; } = new();
    public int Anio { get; set; }
    public List<Feriado> Feriados { get; set; } = new();
}
