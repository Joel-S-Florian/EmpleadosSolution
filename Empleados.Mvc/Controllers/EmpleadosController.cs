using Empleados.Data.Data;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Mvc.Controllers;

[Authorize(Roles = "Admin,RRHH")]
public class EmpleadosController : Controller
{
    private readonly EmpleadosDbContext _context;

    public EmpleadosController(EmpleadosDbContext context)
    {
        _context = context;
    }

    // GET: Empleados
    public async Task<IActionResult> Index(
        string? nombre,
        string? apellido,
        DateTime? fechaContratacion,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Empleados
            .Include(e => e.Departamento)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(e => e.Nombre.Contains(nombre));

        if (!string.IsNullOrWhiteSpace(apellido))
            query = query.Where(e => e.Apellido.Contains(apellido));

        if (fechaContratacion.HasValue)
            query = query.Where(e => e.FechaContratacion.Date == fechaContratacion.Value.Date);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Nombre = nombre;
        ViewBag.Apellido = apellido;
        ViewBag.FechaContratacion = fechaContratacion?.ToString("yyyy-MM-dd");
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return View(items);
    }

    // GET: Empleados/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var empleado = await _context.Empleados
            .Include(e => e.Departamento)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (empleado == null)
            return NotFound();

        return View(empleado);
    }

    // GET: Empleados/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        ViewBag.Departamentos = new SelectList(
            _context.Departamentos.ToList(),
            "Id",
            "Nombre"
        );
        return View();
    }

    // POST: Empleados/Create
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Apellido,FechaNacimiento,Direccion,Telefono,CorreoElectronico,Puesto,Salario,FechaContratacion,DepartamentoId")] Empleado empleado)
    {
        if (ModelState.IsValid)
        {
            empleado.Estado = EstadoEmpleado.Activo;
            _context.Add(empleado);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departamentos = new SelectList(
            _context.Departamentos.ToList(),
            "Id",
            "Nombre"
        );
        return View(empleado);
    }

    // GET: Empleados/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return NotFound();

        ViewBag.Departamentos = new SelectList(
            _context.Departamentos.ToList(),
            "Id",
            "Nombre"
        );
        return View(empleado);
    }

    // POST: Empleados/Edit/5
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Apellido,FechaNacimiento,Direccion,Telefono,CorreoElectronico,Puesto,Salario,FechaContratacion,DepartamentoId")] Empleado empleado)
    {
        if (id != empleado.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(empleado);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Empleados.Any(e => e.Id == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departamentos = new SelectList(
            _context.Departamentos.ToList(),
            "Id",
            "Nombre"
        );
        return View(empleado);
    }

    // POST: Empleados/Desactivar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return NotFound();

        if (empleado.Estado == EstadoEmpleado.Despedido)
        {
            TempData["Error"] = "No se puede desactivar un empleado despedido";
            return RedirectToAction(nameof(Index));
        }

        empleado.Estado = EstadoEmpleado.Inactivo;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Empleado desactivado correctamente";
        return RedirectToAction(nameof(Index));
    }

    // POST: Empleados/Despedir/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Despedir(int id)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return NotFound();

        if (empleado.Estado == EstadoEmpleado.Despedido)
        {
            TempData["Error"] = "El empleado ya está despedido";
            return RedirectToAction(nameof(Index));
        }

        empleado.Estado = EstadoEmpleado.Despedido;
        empleado.FechaTerminacion = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Empleado despedido correctamente";
        return RedirectToAction(nameof(Index));
    }

    // POST: Empleados/SubirFoto/5
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirFoto(int id, IFormFile foto)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return NotFound();

        if (foto == null || foto.Length == 0)
        {
            TempData["Error"] = "Debe seleccionar una imagen";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Validar tipo de archivo
        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();
        if (!extensionesPermitidas.Contains(extension))
        {
            TempData["Error"] = "Solo se permiten archivos de imagen (jpg, jpeg, png, gif)";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Validar tamaño (máximo 5MB)
        if (foto.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "La imagen no puede superar los 5MB";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Crear directorio si no existe
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "empleados");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // Generar nombre único
        var nombreArchivo = $"{id}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        var rutaArchivo = Path.Combine(uploadsFolder, nombreArchivo);

        // Guardar archivo
        using (var stream = new FileStream(rutaArchivo, FileMode.Create))
        {
            await foto.CopyToAsync(stream);
        }

        // Guardar ruta en base de datos
        var rutaRelativa = $"/uploads/empleados/{nombreArchivo}";
        empleado.FotoRuta = rutaRelativa;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Foto subida correctamente";
        return RedirectToAction(nameof(Details), new { id });
    }
}