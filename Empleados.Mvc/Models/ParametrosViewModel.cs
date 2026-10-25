using Empleados.Data.Entities;

namespace Empleados.Mvc.Models;

public class ParametrosViewModel
{
    // Apartados de la pantalla: clave -> (título, ícono)
    public static readonly IReadOnlyDictionary<string, (string Titulo, string Icono)> Secciones = new Dictionary<string, (string, string)>
    {
        ["resumen"] = ("Resumen", "bi-grid"),
        ["tss"] = ("TSS e INFOTEP", "bi-shield-check"),
        ["laboral"] = ("Jornada y recargos", "bi-clock"),
        ["isr"] = ("Escala del ISR", "bi-receipt"),
        ["salarios"] = ("Salarios mínimos", "bi-cash-coin"),
        ["feriados"] = ("Feriados", "bi-calendar-event"),
        ["historial"] = ("Historial", "bi-clock-history")
    };

    public string Seccion { get; set; } = "resumen";
    public DateTime Fecha { get; set; }
    public List<ParametroLegal> Vigentes { get; set; } = new();
    public List<ParametroLegal> Historial { get; set; } = new();
    public List<TramoIsr> EscalaIsr { get; set; } = new();
    public List<SalarioMinimo> SalariosMinimos { get; set; } = new();
    public int Anio { get; set; }
    public List<Feriado> Feriados { get; set; } = new();

    public decimal? Valor(string clave) => Vigentes.FirstOrDefault(p => p.Clave == clave)?.Valor;
    public IEnumerable<ParametroLegal> VigentesDe(string seccion) =>
        Vigentes.Where(p => seccion == "laboral" ? p.Clave.StartsWith("Laboral.") : !p.Clave.StartsWith("Laboral."));
}
