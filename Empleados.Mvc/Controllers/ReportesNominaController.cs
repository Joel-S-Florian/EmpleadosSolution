using Empleados.Data.Data;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Mvc.Controllers;

// Reportes de nómina y personal, con descarga en CSV (Excel) y PDF
[Authorize(Roles = "Admin,RRHH")]
public class ReportesNominaController : Controller
{
    private readonly ReportesNominaService _reportes;
    private readonly EmpleadosDbContext _context;
    private readonly IContextoUsuario _contexto;

    public ReportesNominaController(ReportesNominaService reportes, EmpleadosDbContext context, IContextoUsuario contexto)
    {
        _reportes = reportes;
        _context = context;
        _contexto = contexto;
    }

    // GET: ReportesNomina?tipo=CostoPorDepartamento&anio=2026&mes=10&formato=csv|pdf
    public async Task<IActionResult> Index(TipoReporte tipo = TipoReporte.CostoPorDepartamento, int? anio = null, int? mes = null, string? formato = null)
    {
        var año = anio ?? DateTime.Today.Year;
        var tabla = await _reportes.GenerarAsync(tipo, año, mes);
        var archivo = $"{tipo}-{año}{(mes != null ? $"-{mes:00}" : "")}".ToLowerInvariant();
        switch (formato)
        {
            case "csv":
                return File(ReportesNominaService.Csv(tabla), "text/csv", archivo + ".csv");
            case "pdf":
                var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.Id == _contexto.EmpresaId);
                return empresa == null
                    ? BadRequest("Seleccione una empresa")
                    : File(ReportesNominaService.Pdf(tabla, empresa), "application/pdf", archivo + ".pdf");
        }

        ViewBag.Tipo = tipo;
        ViewBag.Anio = año;
        ViewBag.Mes = mes;
        return View(tabla);
    }
}
