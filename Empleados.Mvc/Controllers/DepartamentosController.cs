using Empleados.Data.Data;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Mvc.Controllers;

[Authorize(Roles = "Admin")]
public class DepartamentosController : Controller
{
    private readonly EmpleadosDbContext _context;

    public DepartamentosController(EmpleadosDbContext context)
    {
        _context = context;
    }

    // GET: Departamentos
    public async Task<IActionResult> Index()
    {
        var departamentos = await _context.Departamentos
            .Include(d => d.Empleados)
            .ToListAsync();
        return View(departamentos);
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
            _context.Add(departamento);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Departamento creado correctamente";
            return RedirectToAction(nameof(Index));
        }
        return View(departamento);
    }

    // GET: Departamentos/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var departamento = await _context.Departamentos.FindAsync(id);
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
            try
            {
                _context.Update(departamento);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Departamento actualizado correctamente";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Departamentos.Any(d => d.Id == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(departamento);
    }

    // POST: Departamentos/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var departamento = await _context.Departamentos
            .Include(d => d.Empleados)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (departamento == null)
            return NotFound();

        // Regla de negocio: No permitir eliminar departamento con empleados asociados
        if (departamento.Empleados.Any())
        {
            TempData["Error"] = "No se puede eliminar un departamento con empleados asociados";
            return RedirectToAction(nameof(Index));
        }

        _context.Departamentos.Remove(departamento);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Departamento eliminado correctamente";
        return RedirectToAction(nameof(Index));
    }
}