using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Mvc.Controllers;

[Authorize(Roles = "Admin")]
public class EmpresasController : Controller
{
    private readonly EmpresaService _empresas;
    private readonly UserManager<IdentityUser> _usuarios;
    private readonly EmpleadosDbContext _context;

    public EmpresasController(EmpresaService empresas, UserManager<IdentityUser> usuarios, EmpleadosDbContext context)
    {
        _empresas = empresas;
        _usuarios = usuarios;
        _context = context;
    }

    // GET: Empresas
    public async Task<IActionResult> Index()
    {
        return View(await _empresas.ListarAsync());
    }

    // GET: Empresas/Create
    public IActionResult Create()
    {
        return View(new EmpresaDto());
    }

    // POST: Empresas/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmpresaDto dto)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _empresas.CrearAsync(dto);
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Edit), new { id = resultado.Valor!.Id });
            }
            ModelState.AgregarError(resultado);
        }
        return View(dto);
    }

    // GET: Empresas/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var empresa = await _empresas.ObtenerAsync(id);
        if (empresa == null)
            return NotFound();

        await CargarUsuariosAsync(empresa.Usuarios.Select(u => u.UsuarioId));
        await CargarResumenAsync(id);
        return View(EmpresaDto.Desde(empresa));
    }

    // POST: Empresas/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmpresaDto dto, string[] usuarioIds)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _empresas.ActualizarAsync(id, dto);
            if (resultado.Tipo == TipoResultado.NoEncontrado)
                return NotFound();
            if (resultado.Ok)
                resultado = await _empresas.AsignarUsuariosAsync(id, usuarioIds);
            if (resultado.Ok)
            {
                TempData["Success"] = "Empresa actualizada correctamente";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AgregarError(resultado);
        }

        await CargarUsuariosAsync(usuarioIds);
        await CargarResumenAsync(id);
        return View(dto);
    }

    // Sin los usuarios del portal: su acceso se da y se quita desde la ficha del empleado
    private async Task CargarUsuariosAsync(IEnumerable<string> asignados)
    {
        var delPortal = _context.Empleados.IgnoreQueryFilters().Where(e => e.UsuarioId != null).Select(e => e.UsuarioId);
        ViewBag.Usuarios = await _usuarios.Users.Where(u => !delPortal.Contains(u.Id)).OrderBy(u => u.Email).Select(u => new { u.Id, u.Email }).ToListAsync();
        ViewBag.Asignados = asignados.ToHashSet();
    }

    private async Task CargarResumenAsync(int id)
    {
        var empleados = _context.Empleados.IgnoreQueryFilters().Where(e => e.EmpresaId == id);
        ViewBag.EmpleadosActivos = await empleados.CountAsync(e => e.Estado == EstadoEmpleado.Activo);
        ViewBag.EmpleadosPortal = await empleados.CountAsync(e => e.UsuarioId != null);
        ViewBag.Departamentos = await _context.Departamentos.IgnoreQueryFilters().CountAsync(d => d.EmpresaId == id);
        ViewBag.UltimaNomina = await _context.Nominas.IgnoreQueryFilters().Where(n => n.EmpresaId == id && n.FechaPago != null)
            .OrderByDescending(n => n.FechaPago).FirstOrDefaultAsync();
    }
}
