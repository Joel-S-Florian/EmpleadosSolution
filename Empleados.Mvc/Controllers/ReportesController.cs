using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Mvc.Controllers;

[Authorize(Roles = "Admin,RRHH")]
public class ReportesController : Controller
{
    private readonly EmpleadosDbContext _context;

    public ReportesController(EmpleadosDbContext context)
    {
        _context = context;
    }

    // GET: Reportes/Index - vista con tarjetas Bootstrap mostrando métricas por departamento
    public async Task<IActionResult> Index()
    {
        // Misma consulta optimizada que API: una sola query con GroupJoin
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

        return View(reporte);
    }
}
