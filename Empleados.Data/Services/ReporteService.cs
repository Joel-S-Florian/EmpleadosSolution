using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Services;

public class ReporteService
{
    private readonly EmpleadosDbContext _context;

    public ReporteService(EmpleadosDbContext context)
    {
        _context = context;
    }

    // Una sola consulta: GroupJoin -> LEFT JOIN + GROUP BY. Incluye departamentos sin empleados.
    public Task<List<ReporteDepartamentoDto>> PorDepartamentoAsync() =>
        _context.Departamentos
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
}
