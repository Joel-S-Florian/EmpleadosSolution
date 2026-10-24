using Empleados.Data.DTOs;
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

    public EmpresasController(EmpresaService empresas, UserManager<IdentityUser> usuarios)
    {
        _empresas = empresas;
        _usuarios = usuarios;
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
        return View(dto);
    }

    private async Task CargarUsuariosAsync(IEnumerable<string> asignados)
    {
        ViewBag.Usuarios = await _usuarios.Users.OrderBy(u => u.Email).Select(u => new { u.Id, u.Email }).ToListAsync();
        ViewBag.Asignados = asignados.ToHashSet();
    }
}
