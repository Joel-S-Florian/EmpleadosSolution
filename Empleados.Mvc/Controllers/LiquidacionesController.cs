using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Mvc.Controllers;

// Prestaciones laborales: Admin y RRHH calculan; solo el Admin marca el pago (el empleado sale de la empresa)
[Authorize(Roles = "Admin,RRHH")]
public class LiquidacionesController : Controller
{
    private readonly LiquidacionService _liquidaciones;
    private readonly EmpleadoService _empleados;
    private readonly EmpleadosDbContext _context;

    public LiquidacionesController(LiquidacionService liquidaciones, EmpleadoService empleados, EmpleadosDbContext context)
    {
        _liquidaciones = liquidaciones;
        _empleados = empleados;
        _context = context;
    }

    // GET: Liquidaciones?empleadoId=5
    public async Task<IActionResult> Index(int? empleadoId)
    {
        return await Mostrar(new LiquidacionDto { EmpleadoId = empleadoId ?? 0 });
    }

    // POST: Liquidaciones/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(LiquidacionDto dto)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _liquidaciones.CrearAsync(dto);
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Detalle), new { id = resultado.Valor!.Id });
            }
            ModelState.AgregarError(resultado);
        }
        return await Mostrar(dto);
    }

    // GET: Liquidaciones/Detalle/5
    public async Task<IActionResult> Detalle(int id)
    {
        var liquidacion = await _liquidaciones.ObtenerAsync(id);
        return liquidacion == null ? NotFound() : View(liquidacion);
    }

    // GET: Liquidaciones/Pdf/5
    public async Task<IActionResult> Pdf(int id)
    {
        var liquidacion = await _liquidaciones.ObtenerAsync(id);
        if (liquidacion == null)
            return NotFound();
        var empresa = await _context.Empresas.FirstAsync(e => e.Id == liquidacion.EmpresaId);
        return File(LiquidacionPdf.Generar(empresa, liquidacion), "application/pdf", $"liquidacion-{liquidacion.Id}.pdf");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pagar(int id, DateTime? fechaPago)
    {
        var resultado = await _liquidaciones.PagarAsync(id, fechaPago ?? DateTime.Today);
        if (resultado.Tipo == TipoResultado.NoEncontrado)
            return NotFound();
        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await _liquidaciones.EliminarAsync(id);
        if (resultado.Tipo == TipoResultado.NoEncontrado)
            return NotFound();
        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return resultado.Ok ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Detalle), new { id });
    }

    private async Task<IActionResult> Mostrar(LiquidacionDto nuevo)
    {
        var empleados = await _empleados.ParaSeleccionAsync(incluirDespedidos: true);
        ViewBag.Empleados = new SelectList(empleados.Select(e => new { e.Id, Texto = $"{e.Nombre} {e.Apellido}" }), "Id", "Texto", nuevo.EmpleadoId);
        ViewBag.Nuevo = nuevo;
        return View("Index", await _liquidaciones.ListarAsync());
    }
}
