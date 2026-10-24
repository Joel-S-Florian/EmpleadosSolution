using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Empleados.Mvc.Controllers;

[Authorize(Roles = "Admin,RRHH")]
public class PuestosController : Controller
{
    private readonly PuestoService _puestos;
    private readonly DepartamentoService _departamentos;

    public PuestosController(PuestoService puestos, DepartamentoService departamentos)
    {
        _puestos = puestos;
        _departamentos = departamentos;
    }

    // GET: Puestos
    public async Task<IActionResult> Index(int? departamentoId)
    {
        var puestos = await _puestos.ListarAsync(departamentoId);
        await CargarDepartamentosAsync(departamentoId);
        ViewBag.FiltroDepartamentoId = departamentoId;
        return View(puestos);
    }

    // GET: Puestos/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var puesto = await _puestos.ObtenerAsync(id);
        if (puesto == null) return NotFound();
        return View(puesto);
    }

    // GET: Puestos/Create
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await CargarDepartamentosAsync(null);
        return View();
    }

    // POST: Puestos/Create
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion,DepartamentoId")] Puesto puesto)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _puestos.CrearAsync(ADto(puesto));
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AgregarError(resultado);
        }
        await CargarDepartamentosAsync(puesto.DepartamentoId);
        return View(puesto);
    }

    // GET: Puestos/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var puesto = await _puestos.ObtenerAsync(id);
        if (puesto == null) return NotFound();
        await CargarDepartamentosAsync(puesto.DepartamentoId);
        return View(puesto);
    }

    // POST: Puestos/Edit/5
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,DepartamentoId")] Puesto puesto)
    {
        if (id != puesto.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var resultado = await _puestos.ActualizarAsync(id, ADto(puesto));
            if (resultado.Tipo == TipoResultado.NoEncontrado) return NotFound();
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AgregarError(resultado);
        }
        await CargarDepartamentosAsync(puesto.DepartamentoId);
        return View(puesto);
    }

    // POST: Puestos/Delete/5
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await _puestos.EliminarAsync(id);
        if (resultado.Tipo == TipoResultado.NoEncontrado) return NotFound();

        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    // GET: Puestos/GetByDepartamento?departamentoId=1 - para el select dinámico de Empleados Create/Edit
    [HttpGet]
    public async Task<IActionResult> GetByDepartamento(int departamentoId)
    {
        var puestos = await _puestos.ListarAsync(departamentoId);
        return Json(puestos.Select(p => new { id = p.Id, nombre = p.Nombre }));
    }

    private async Task CargarDepartamentosAsync(int? seleccionado) =>
        ViewBag.Departamentos = new SelectList(await _departamentos.ListarAsync(), "Id", "Nombre", seleccionado);

    private static CrearPuestoDto ADto(Puesto p) =>
        new() { Nombre = p.Nombre, Descripcion = p.Descripcion, DepartamentoId = p.DepartamentoId };
}
