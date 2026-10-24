using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Mvc.Controllers;

[Authorize(Roles = "Admin")]
public class DepartamentosController : Controller
{
    private readonly DepartamentoService _departamentos;

    public DepartamentosController(DepartamentoService departamentos)
    {
        _departamentos = departamentos;
    }

    // GET: Departamentos
    public async Task<IActionResult> Index()
    {
        return View(await _departamentos.ListarAsync(conEmpleados: true));
    }

    // GET: Departamentos/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Departamentos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion")] Departamento departamento)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _departamentos.CrearAsync(ADto(departamento));
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AgregarError(resultado);
        }
        return View(departamento);
    }

    // GET: Departamentos/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var departamento = await _departamentos.ObtenerAsync(id);
        if (departamento == null)
            return NotFound();
        return View(departamento);
    }

    // POST: Departamentos/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion")] Departamento departamento)
    {
        if (id != departamento.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            var resultado = await _departamentos.ActualizarAsync(id, ADto(departamento));
            if (resultado.Tipo == TipoResultado.NoEncontrado)
                return NotFound();
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AgregarError(resultado);
        }
        return View(departamento);
    }

    // POST: Departamentos/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await _departamentos.EliminarAsync(id);
        if (resultado.Tipo == TipoResultado.NoEncontrado)
            return NotFound();

        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    private static CrearDepartamentoDto ADto(Departamento d) => new() { Nombre = d.Nombre, Descripcion = d.Descripcion };
}
