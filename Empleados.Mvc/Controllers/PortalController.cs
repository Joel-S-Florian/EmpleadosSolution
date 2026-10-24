using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Mvc.Controllers;

// Portal del empleado: sus recibos, su saldo de vacaciones, sus solicitudes y su carta de trabajo
[Authorize(Roles = SoloPortalFilter.RolEmpleado)]
public class PortalController : Controller
{
    private readonly PortalService _portal;
    private readonly IncidenciaService _incidencias;
    private readonly EmpleadosDbContext _context;

    public PortalController(PortalService portal, IncidenciaService incidencias, EmpleadosDbContext context)
    {
        _portal = portal;
        _incidencias = incidencias;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var empleado = await _portal.EmpleadoAsync();
        if (empleado == null)
            return View("SinEmpleado");
        return await Mostrar(empleado, new IncidenciaDto { Tipo = TipoIncidencia.Vacaciones });
    }

    // POST: Portal/Solicitar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(IncidenciaDto dto)
    {
        var empleado = await _portal.EmpleadoAsync();
        if (empleado == null)
            return View("SinEmpleado");
        if (ModelState.IsValid)
        {
            var resultado = await _portal.SolicitarAsync(empleado, dto);
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AgregarError(resultado);
        }
        return await Mostrar(empleado, dto);
    }

    // GET: Portal/Recibo/5
    public async Task<IActionResult> Recibo(int id)
    {
        var empleado = await _portal.EmpleadoAsync();
        var recibo = empleado == null ? null : await _portal.ReciboAsync(empleado.Id, id);
        if (recibo == null)
            return NotFound();
        var empresa = await _context.Empresas.FirstAsync(e => e.Id == recibo.EmpresaId);
        return File(ReciboPdf.Generar(empresa, recibo.Nomina!, new[] { recibo }), "application/pdf", $"recibo-{recibo.Nomina!.Anio}-{recibo.Nomina.Mes:00}.pdf");
    }

    // GET: Portal/CartaTrabajo
    public async Task<IActionResult> CartaTrabajo()
    {
        var empleado = await _portal.EmpleadoAsync();
        if (empleado == null)
            return NotFound();
        var empresa = await _context.Empresas.FirstAsync(e => e.Id == empleado.EmpresaId);
        return File(CartaTrabajoPdf.Generar(empresa, empleado, DateTime.Today), "application/pdf", "carta-de-trabajo.pdf");
    }

    private async Task<IActionResult> Mostrar(Empleado empleado, IncidenciaDto nueva)
    {
        ViewBag.Recibos = await _portal.RecibosAsync(empleado.Id);
        ViewBag.Vacaciones = await _incidencias.SaldoVacacionesAsync(empleado.Id);
        ViewBag.Solicitudes = (await _incidencias.ListarAsync(empleado.Id)).Take(15).ToList();
        ViewBag.Nueva = nueva;
        return View("Index", empleado);
    }
}

// Los usuarios del portal solo entran a su portal (las páginas de cuenta de Identity no pasan por aquí)
public class SoloPortalFilter : IActionFilter
{
    public const string RolEmpleado = "Empleado";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var usuario = context.HttpContext.User;
        if (usuario.IsInRole(RolEmpleado) && !usuario.IsInRole("Admin") && !usuario.IsInRole("RRHH") &&
            context.Controller is not PortalController)
            context.Result = new RedirectToActionResult(nameof(PortalController.Index), "Portal", null);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
