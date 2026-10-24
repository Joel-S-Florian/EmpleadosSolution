using Empleados.Data.Entities;
using Empleados.Data.Services;
using Empleados.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Mvc.Controllers;

// Parámetros legales versionados: tasas TSS, topes, escala ISR, salarios mínimos y feriados
[Authorize(Roles = "Admin")]
public class ParametrosController : Controller
{
    private readonly ParametrosLegalesService _parametros;

    public ParametrosController(ParametrosLegalesService parametros)
    {
        _parametros = parametros;
    }

    // GET: Parametros?fecha=2026-03-01&anio=2026
    public async Task<IActionResult> Index(DateTime? fecha, int? anio)
    {
        var dia = fecha?.Date ?? DateTime.Today;
        var modelo = new ParametrosViewModel
        {
            Fecha = dia,
            Vigentes = await _parametros.VigentesAsync(dia),
            Historial = await _parametros.HistorialAsync(),
            EscalaIsr = await _parametros.EscalaIsrAsync(dia),
            SalariosMinimos = await _parametros.SalariosMinimosAsync(),
            Anio = anio ?? dia.Year,
        };
        modelo.Feriados = await _parametros.FeriadosAsync(modelo.Anio);
        return View(modelo);
    }

    // POST: Parametros/NuevaVigencia
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevaVigencia(string clave, decimal valor, DateTime vigenteDesde, string? fuente)
    {
        Avisar(await _parametros.AgregarVigenciaAsync(clave, valor, vigenteDesde, fuente));
        return RedirectToAction(nameof(Index));
    }

    // POST: Parametros/NuevoSalarioMinimo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevoSalarioMinimo(TamanoEmpresa tamano, decimal monto, DateTime vigenteDesde)
    {
        Avisar(await _parametros.AgregarSalarioMinimoAsync(tamano, monto, vigenteDesde));
        return RedirectToAction(nameof(Index));
    }

    // POST: Parametros/NuevoFeriado
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevoFeriado(DateTime fecha, string nombre)
    {
        Avisar(await _parametros.AgregarFeriadoAsync(fecha, nombre));
        return RedirectToAction(nameof(Index), new { anio = fecha.Year });
    }

    // POST: Parametros/GenerarFeriados?anio=2027
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerarFeriados(int anio)
    {
        Avisar(await _parametros.GenerarFeriadosAsync(anio));
        return RedirectToAction(nameof(Index), new { anio });
    }

    // POST: Parametros/EliminarFeriado/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarFeriado(int id, int anio)
    {
        Avisar(await _parametros.EliminarFeriadoAsync(id));
        return RedirectToAction(nameof(Index), new { anio });
    }

    private void Avisar(Resultado resultado) =>
        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
}
