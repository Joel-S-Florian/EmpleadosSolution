using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly ReporteService _reportes;

    public ReportesController(ReporteService reportes)
    {
        _reportes = reportes;
    }

    // GET: api/Reportes/Departamentos - métricas agregadas por departamento
    [HttpGet("Departamentos")]
    public async Task<IActionResult> GetReporteDepartamentos()
    {
        return Ok(await _reportes.PorDepartamentoAsync());
    }

    // GET: api/Reportes/nomina/CostoPorDepartamento?anio=2026&mes=10
    [Authorize(Roles = "Admin,RRHH")]
    [HttpGet("nomina/{tipo}")]
    public async Task<IActionResult> GetReporteNomina(TipoReporte tipo, [FromQuery] int? anio, [FromQuery] int? mes, [FromServices] ReportesNominaService reportes)
    {
        var tabla = await reportes.GenerarAsync(tipo, anio ?? DateTime.Today.Year, mes);
        return Ok(new { tabla.Titulo, tabla.Subtitulo, Columnas = tabla.Columnas.Select(c => c.Titulo), tabla.Filas, tabla.Totales });
    }
}
