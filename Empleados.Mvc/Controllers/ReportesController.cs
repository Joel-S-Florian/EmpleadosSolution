using Empleados.Data.Data;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Mvc.Controllers;

// Centro de reportes: personal, nómina y asistencia, con filtros y descarga en Excel, CSV y PDF
[Authorize(Roles = "Admin,RRHH")]
public class ReportesController : Controller
{
    private readonly GeneradorReportes _reportes;
    private readonly EmpleadosDbContext _context;
    private readonly IContextoUsuario _contexto;

    public ReportesController(GeneradorReportes reportes, EmpleadosDbContext context, IContextoUsuario contexto)
    {
        _reportes = reportes;
        _context = context;
        _contexto = contexto;
    }

    // GET: Reportes?tipo=CostoPorDepartamento&anio=2026&mes=10&departamentoId=1&estado=Activo&nominaId=5&formato=xlsx|csv|pdf
    public async Task<IActionResult> Index(TipoReporte tipo = TipoReporte.PlantillaEmpleados, int? anio = null, int? mes = null,
        int? departamentoId = null, EstadoEmpleado? estado = null, int? nominaId = null, string? formato = null)
    {
        var info = GeneradorReportes.Catalogo[tipo];
        var nominas = await _context.Nominas.Where(n => n.Estado != EstadoNomina.Borrador)
            .OrderByDescending(n => n.FechaFin).ThenByDescending(n => n.Id).ToListAsync();
        if (info.Nomina && nominaId == null)
            nominaId = nominas.FirstOrDefault()?.Id;

        var filtro = new FiltroReporte(anio ?? DateTime.Today.Year, info.Mes ? mes : null, info.Departamento ? departamentoId : null,
            info.Estado ? estado : null, info.Nomina ? nominaId : null);
        var tabla = await _reportes.GenerarAsync(tipo, filtro);

        if (formato != null)
        {
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.Id == _contexto.EmpresaId);
            if (empresa == null)
                return BadRequest("Seleccione una empresa");
            var archivo = $"{tipo}-{filtro.Anio}{(filtro.Mes != null ? $"-{filtro.Mes:00}" : "")}".ToLowerInvariant();
            switch (formato)
            {
                case "xlsx":
                    return File(ExcelReporte.Generar(tabla, empresa), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", archivo + ".xlsx");
                case "csv":
                    return File(GeneradorReportes.Csv(tabla), "text/csv", archivo + ".csv");
                case "pdf":
                    return File(GeneradorReportes.Pdf(tabla, empresa), "application/pdf", archivo + ".pdf");
            }
        }

        ViewBag.Tipo = tipo;
        ViewBag.Filtro = filtro;
        ViewBag.Departamentos = new SelectList(await _context.Departamentos.OrderBy(d => d.Nombre).ToListAsync(), "Id", "Nombre", filtro.DepartamentoId);
        ViewBag.Nominas = new SelectList(nominas.Select(n => new { n.Id, Texto = $"{n.Descripcion} · {Textos.De(n.Estado)}" }), "Id", "Texto", filtro.NominaId);
        return View(tabla);
    }
}
