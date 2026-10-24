using Empleados.Data.DTOs;
using Empleados.Data.Services;

namespace Empleados.Mvc.Models;

public class SimuladorViewModel
{
    public SimulacionNominaDto Entrada { get; set; } = new();
    public SimulacionNomina? Resultado { get; set; }
}
