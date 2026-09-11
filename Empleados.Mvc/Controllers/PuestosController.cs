using Empleados.Data.Data;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Mvc.Controllers;

[Authorize(Roles = "Admin,RRHH")]
public class PuestosController : Controller
{
    private readonly EmpleadosDbContext _context;

    public PuestosController(EmpleadosDbContext context)
    {
        _context = context;
    }

    // GET: Puestos
    public async Task<IActionResult> Index(int? departamentoId)
    {
        var query = _context.Puestos.Include(p => p.Departamento).AsQueryable();
        if (departamentoId.HasValue)
            query = query.Where(p => p.DepartamentoId == departamentoId.Value);

        var puestos = await query.OrderBy(p => p.Nombre).ToListAsync();
        ViewBag.Departamentos = new SelectList(await _context.Departamentos.ToListAsync(), "Id", "Nombre", departamentoId);
        ViewBag.FiltroDepartamentoId = departamentoId;
        return View(puestos);
    }

    // GET: Puestos/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var puesto = await _context.Puestos.Include(p => p.Departamento).FirstOrDefaultAsync(p => p.Id == id);
        if (puesto == null) return NotFound();
        return View(puesto);
    }

    // GET: Puestos/Create
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Departamentos = new SelectList(await _context.Departamentos.ToListAsync(), "Id", "Nombre");
        return View();
    }

    // POST: Puestos/Create
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion,DepartamentoId")] Puesto puesto)
    {
        // Validar existencia del departamento antes de crear puesto
        var deptExists = await _context.Departamentos.AnyAsync(d => d.Id == puesto.DepartamentoId);
        if (!deptExists)
            ModelState.AddModelError("DepartamentoId", "El departamento no existe");

        if (ModelState.IsValid)
        {
            var duplicate = await _context.Puestos.AnyAsync(p => p.Nombre == puesto.Nombre && p.DepartamentoId == puesto.DepartamentoId);
            if (duplicate)
            {
                ModelState.AddModelError("Nombre", "Ya existe un puesto con ese nombre en el departamento");
            }
            else
            {
                _context.Add(puesto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Puesto creado correctamente";
                return RedirectToAction(nameof(Index));
            }
        }
        ViewBag.Departamentos = new SelectList(await _context.Departamentos.ToListAsync(), "Id", "Nombre", puesto.DepartamentoId);
        return View(puesto);
    }

    // GET: Puestos/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var puesto = await _context.Puestos.FindAsync(id);
        if (puesto == null) return NotFound();
        ViewBag.Departamentos = new SelectList(await _context.Departamentos.ToListAsync(), "Id", "Nombre", puesto.DepartamentoId);
        return View(puesto);
    }

    // POST: Puestos/Edit/5
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,DepartamentoId")] Puesto puesto)
    {
        if (id != puesto.Id) return NotFound();

        var deptExists = await _context.Departamentos.AnyAsync(d => d.Id == puesto.DepartamentoId);
        if (!deptExists)
            ModelState.AddModelError("DepartamentoId", "El departamento no existe");

        if (ModelState.IsValid)
        {
            var duplicate = await _context.Puestos.AnyAsync(p => p.Nombre == puesto.Nombre && p.DepartamentoId == puesto.DepartamentoId && p.Id != id);
            if (duplicate)
            {
                ModelState.AddModelError("Nombre", "Ya existe otro puesto con ese nombre en el departamento");
            }
            else
            {
                try
                {
                    _context.Update(puesto);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Puesto actualizado correctamente";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Puestos.AnyAsync(p => p.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
        }
        ViewBag.Departamentos = new SelectList(await _context.Departamentos.ToListAsync(), "Id", "Nombre", puesto.DepartamentoId);
        return View(puesto);
    }

    // POST: Puestos/Delete/5
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var puesto = await _context.Puestos.Include(p => p.Empleados).FirstOrDefaultAsync(p => p.Id == id);
        if (puesto == null) return NotFound();
        if (puesto.Empleados.Any())
        {
            TempData["Error"] = "No se puede eliminar un puesto que tiene empleados asociados";
            return RedirectToAction(nameof(Index));
        }
        _context.Puestos.Remove(puesto);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Puesto eliminado correctamente";
        return RedirectToAction(nameof(Index));
    }

    // GET: Puestos/GetByDepartamento?departamentoId=1 - para JS dinámico en Empleados Create/Edit
    [AllowAnonymous] // permite polling sin rol específico, pero requiere autenticación base
    [HttpGet]
    public async Task<IActionResult> GetByDepartamento(int departamentoId)
    {
        var puestos = await _context.Puestos
            .Where(p => p.DepartamentoId == departamentoId)
            .Select(p => new { id = p.Id, nombre = p.Nombre })
            .ToListAsync();
        return Json(puestos);
    }
}
