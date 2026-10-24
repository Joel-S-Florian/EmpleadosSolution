using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Mvc.Controllers;

[Authorize(Roles = "Admin,RRHH")]
public class ReportesController : Controller
{
    private readonly ReporteService _reportes;

    public ReportesController(ReporteService reportes)
    {
        _reportes = reportes;
    }

    // GET: Reportes/Index - tarjetas con métricas por departamento
    public async Task<IActionResult> Index()
    {
        return View(await _reportes.PorDepartamentoAsync());
    }
}
