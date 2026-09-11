using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly EmpleadosDbContext _context;

    public ReportesController(EmpleadosDbContext context)
    {
        _context = context;
    }

    // GET: api/Reportes/Departamentos - métricas agregadas en tiempo real con una sola consulta LINQ optimizada (GroupBy/GroupJoin)
    [HttpGet("Departamentos")]
    public async Task<IActionResult> GetReporteDepartamentos()
    {
        // Consulta optimizada: una sola query con GroupJoin -> traducido a LEFT JOIN + GROUP BY en SQL
        // Incluye departamentos sin empleados (TotalEmpleados = 0, NominaMensual = 0)
        var reporte = await _context.Departamentos
            .GroupJoin(
                _context.Empleados,
                d => d.Id,
                e => e.DepartamentoId,
                (departamento, empleados) => new ReporteDepartamentoDto
                {
                    Nombre = departamento.Nombre,
                    TotalEmpleados = empleados.Count(),
                    Activos = empleados.Count(e => e.Estado == EstadoEmpleado.Activo),
                    NominaMensual = empleados
                        .Where(e => e.Estado == EstadoEmpleado.Activo)
                        .Sum(e => (decimal?)e.Salario) ?? 0
                })
            .OrderBy(r => r.Nombre)
            .ToListAsync();

        return Ok(reporte);
    }
}
