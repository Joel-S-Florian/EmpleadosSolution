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
}
