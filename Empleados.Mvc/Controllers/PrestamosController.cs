using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Empleados.Mvc.Controllers;

[Authorize(Roles = "Admin,RRHH")]
public class PrestamosController : Controller
{
    private readonly PrestamoService _prestamos;
    private readonly EmpleadoService _empleados;

    public PrestamosController(PrestamoService prestamos, EmpleadoService empleados)
    {
        _prestamos = prestamos;
        _empleados = empleados;
    }

    // GET: Prestamos?empleadoId=5
    public async Task<IActionResult> Index(int? empleadoId)
    {
        return await Mostrar(new PrestamoDto { EmpleadoId = empleadoId ?? 0 }, empleadoId);
    }

    // POST: Prestamos/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(PrestamoDto dto)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _prestamos.CrearAsync(dto);
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AgregarError(resultado);
        }
        return await Mostrar(dto, null);
    }

    // POST: Prestamos/Cancelar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var resultado = await _prestamos.CancelarAsync(id);
        if (resultado.Tipo == TipoResultado.NoEncontrado)
            return NotFound();
        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Mostrar(PrestamoDto nuevo, int? filtroEmpleado)
    {
        var empleados = (await _empleados.ListarAsync(null, null, null, pageSize: 100)).Items
            .Where(e => e.Estado != EstadoEmpleado.Despedido)
            .OrderBy(e => e.Nombre).ThenBy(e => e.Apellido);
        ViewBag.Empleados = new SelectList(empleados.Select(e => new { e.Id, Texto = $"{e.Nombre} {e.Apellido}" }), "Id", "Texto", nuevo.EmpleadoId);
        ViewBag.Nuevo = nuevo;
        return View("Index", await _prestamos.ListarAsync(filtroEmpleado));
    }
}
