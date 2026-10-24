using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Empleados.Mvc.Controllers;

// Salario de Navidad (art. 219) y Bonificación (art. 223)
[Authorize(Roles = "Admin,RRHH")]
public class PagosEspecialesController : Controller
{
    private readonly CicloNominaService _ciclo;
    private readonly BonificacionService _bonificacion;

    public PagosEspecialesController(CicloNominaService ciclo, BonificacionService bonificacion)
    {
        _ciclo = ciclo;
        _bonificacion = bonificacion;
    }

    // GET: PagosEspeciales?anioFiscal=2025&utilidad=1000000 (con utilidad muestra el reparto de la bonificación)
    public async Task<IActionResult> Index(int? anioFiscal, decimal? utilidad)
    {
        RepartoBonificacion? reparto = null;
        if (utilidad != null)
        {
            var calculo = await _bonificacion.CalcularAsync(anioFiscal ?? DateTime.Today.Year - 1, utilidad.Value);
            if (calculo.Ok)
                reparto = calculo.Valor;
            else
                TempData["Error"] = calculo.Mensaje;
        }

        var nominas = await _ciclo.ListarAsync();
        ViewBag.AnioFiscal = anioFiscal ?? DateTime.Today.Year - 1;
        ViewBag.Utilidad = utilidad;
        ViewBag.Navidad = nominas.Where(n => n.Tipo == TipoNomina.SalarioNavidad).ToList();
        ViewBag.NominasDestino = new SelectList(nominas.Where(n => n.Tipo == TipoNomina.Regular && n.Editable)
            .Select(n => new { n.Id, n.Descripcion }), "Id", "Descripcion");
        return View(reparto);
    }

    // POST: PagosEspeciales/CrearSalarioNavidad
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearSalarioNavidad(int anio)
    {
        var resultado = await _ciclo.CrearSalarioNavidadAsync(anio);
        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return resultado.Ok
            ? RedirectToAction("Detalle", "Nominas", new { id = resultado.Valor!.Id })
            : RedirectToAction(nameof(Index));
    }

    // POST: PagosEspeciales/AplicarBonificacion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AplicarBonificacion(int anioFiscal, decimal utilidad, int nominaId)
    {
        var resultado = await _bonificacion.AplicarAsync(anioFiscal, utilidad, nominaId);
        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return resultado.Ok
            ? RedirectToAction("Detalle", "Nominas", new { id = nominaId })
            : RedirectToAction(nameof(Index), new { anioFiscal, utilidad });
    }
}
