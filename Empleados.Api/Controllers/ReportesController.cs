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

    // GET: api/Reportes/catalogo - los reportes disponibles y los filtros que usa cada uno
    [Authorize(Roles = "Admin,RRHH")]
    [HttpGet("catalogo")]
    public IActionResult GetCatalogo() =>
        Ok(GeneradorReportes.Catalogo.Select(r => new { Tipo = r.Key.ToString(), Nombre = Textos.De(r.Key), r.Value.Categoria, r.Value.Descripcion,
            Filtros = new { r.Value.Anio, r.Value.Mes, r.Value.Departamento, r.Value.Estado, r.Value.Nomina } }));

    // GET: api/Reportes/CostoPorDepartamento?anio=2026&mes=10&departamentoId=1&estado=Activo&nominaId=5
    [Authorize(Roles = "Admin,RRHH")]
    [HttpGet("{tipo}")]
    public async Task<IActionResult> GetReporte(TipoReporte tipo, [FromQuery] int? anio, [FromQuery] int? mes, [FromQuery] int? departamentoId,
        [FromQuery] Empleados.Data.Entities.EstadoEmpleado? estado, [FromQuery] int? nominaId, [FromServices] GeneradorReportes reportes)
    {
        var tabla = await reportes.GenerarAsync(tipo, new FiltroReporte(anio ?? DateTime.Today.Year, mes, departamentoId, estado, nominaId));
        return Ok(new { tabla.Titulo, tabla.Subtitulo, Columnas = tabla.Columnas.Select(c => c.Titulo), tabla.Filas, tabla.Totales });
    }
}
