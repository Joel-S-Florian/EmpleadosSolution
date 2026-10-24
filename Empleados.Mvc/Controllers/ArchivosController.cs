using Empleados.Data.Data;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Empleados.Mvc.Controllers;

// Archivo de la TSS, pago al banco y planilla de personal
[Authorize(Roles = "Admin,RRHH")]
public class ArchivosController : Controller
{
    private readonly ArchivosService _archivos;
    private readonly CicloNominaService _ciclo;
    private readonly EmpleadosDbContext _context;

    public ArchivosController(ArchivosService archivos, CicloNominaService ciclo, EmpleadosDbContext context)
    {
        _archivos = archivos;
        _ciclo = ciclo;
        _context = context;
    }

    // GET: Archivos?anio=2026&mes=10 (con mes muestra la autodeterminación de la TSS)
    public async Task<IActionResult> Index(int? anio, int? mes)
    {
        ReporteTss? reporte = null;
        if (anio != null && mes != null)
        {
            var resultado = await _archivos.AutodeterminacionAsync(anio.Value, mes.Value);
            if (resultado.Ok)
                reporte = resultado.Valor;
            else
                TempData["Error"] = resultado.Mensaje;
        }
        var anterior = DateTime.Today.AddMonths(-1);
        ViewBag.Anio = anio ?? anterior.Year;
        ViewBag.Mes = mes ?? anterior.Month;
        ViewBag.Nominas = new SelectList((await _ciclo.ListarAsync()).Where(n => !n.Editable).Select(n => new { n.Id, n.Descripcion }), "Id", "Descripcion");
        ViewBag.Bancos = new SelectList(await _context.Bancos.OrderBy(b => b.Id).ToListAsync(), "Id", "Nombre");
        return View(reporte);
    }

    // GET: Archivos/Tss?anio=2026&mes=10
    public async Task<IActionResult> Tss(int anio, int mes)
    {
        var resultado = await _archivos.AutodeterminacionAsync(anio, mes);
        if (!resultado.Ok || resultado.Valor!.Contenido == null)
        {
            TempData["Error"] = resultado.Mensaje ?? "Ningún trabajador tiene documento para el archivo";
            return RedirectToAction(nameof(Index), new { anio, mes });
        }
        return File(Encoding.ASCII.GetBytes(resultado.Valor.Contenido), "text/plain", resultado.Valor.NombreArchivo);
    }

    // GET: Archivos/Dispersion?nominaId=5&bancoId=1
    public async Task<IActionResult> Dispersion(int nominaId, int? bancoId)
    {
        var resultado = await _archivos.DispersionAsync(nominaId, bancoId);
        if (!resultado.Ok)
        {
            TempData["Error"] = resultado.Mensaje;
            return RedirectToAction(nameof(Index));
        }
        return File(resultado.Valor!.Contenido, "text/csv", resultado.Valor.Nombre);
    }

    // GET: Archivos/Planilla
    public async Task<IActionResult> Planilla()
    {
        var archivo = await _archivos.PlanillaPersonalAsync();
        return File(archivo.Contenido, "text/csv", archivo.Nombre);
    }
}
